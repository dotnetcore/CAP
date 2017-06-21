using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Job
{
    public class CronJobProcessor : IJobProcessor
    {
        private ILogger _logger;
        private IServiceProvider _provider;
        private DefaultCronJobRegistry _jobRegistry;

        public CronJobProcessor(
            DefaultCronJobRegistry jobRegistry,
            ILogger<CronJobProcessor> logger,
            IServiceProvider provider) {

            _jobRegistry = jobRegistry;
            _logger = logger;
            _provider = provider;
        }

        public override string ToString() => nameof(CronJobProcessor);

        public Task ProcessAsync(ProcessingContext context) {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return ProcessCoreAsync(context);
        }

        private async Task ProcessCoreAsync(ProcessingContext context) {
            //var storage = context.Storage;
            //var jobs = await GetJobsAsync(storage);

            var jobs = GetJobs();
            if (!jobs.Any()) {
                _logger.CronJobsNotFound();

                // This will cancel this processor.
                throw new OperationCanceledException();
            }
            _logger.CronJobsScheduling(jobs);

            context.ThrowIfStopping();

            var computedJobs = Compute(jobs, context.CronJobRegistry.Build());
            if (context.IsStopping) {
                return;
            }

            await Task.WhenAll(computedJobs.Select(j => RunAsync(j, context)));
        }

        private async Task RunAsync(ComputedCronJob computedJob, ProcessingContext context) {
            //var storage = context.Storage;
            var retryBehavior = computedJob.RetryBehavior;

            while (!context.IsStopping) {
                var now = DateTime.UtcNow;

                var due = ComputeDue(computedJob, now);
                var timeSpan = due - now;

                if (timeSpan.TotalSeconds > 0) {
                    await context.WaitAsync(timeSpan);
                }

                context.ThrowIfStopping();

                using (var scopedContext = context.CreateScope()) {
                    var provider = scopedContext.Provider;

                    var job = provider.GetService<IJob>();
                    var success = true;

                    try {
                        var sw = Stopwatch.StartNew();
                        await job.ExecuteAsync();
                        sw.Stop();
                        computedJob.Retries = 0;
                        _logger.CronJobExecuted(computedJob.Job.Name, sw.Elapsed.TotalSeconds);
                    }
                    catch (Exception ex) {
                        success = false;
                        if (computedJob.Retries == 0) {
                            computedJob.FirstTry = DateTime.UtcNow;
                        }
                        computedJob.Retries++;
                        _logger.CronJobFailed(computedJob.Job.Name, ex);
                    }

                    if (success) {
                        //var connection = provider.GetRequiredService<IStorageConnection>();
                        //await connection.AttachCronJobAsync(computedJob.Job);

                        computedJob.Update(DateTime.UtcNow);

                        //await connection.UpdateCronJobAsync(computedJob.Job);
                    }
                }
            }
        }

        private DateTime ComputeDue(ComputedCronJob computedJob, DateTime now) {
            computedJob.UpdateNext(now);

            var retryBehavior = computedJob.RetryBehavior ?? RetryBehavior.DefaultRetry;
            var retries = computedJob.Retries;

            if (retries == 0) {
                return computedJob.Next;
            }

            var realNext = computedJob.Schedule.GetNextOccurrence(now);

            if (!retryBehavior.Retry) {
                // No retry. If job failed before, we don't care, just schedule it next as usual.
                return realNext;
            }

            if (retries >= retryBehavior.RetryCount) {
                // Max retries. Just schedule it for the next occurance.
                return realNext;
            }

            // Delay a bit.
            return computedJob.FirstTry.AddSeconds(retryBehavior.RetryIn(retries));
        }

        private CronJob[] GetJobs() {
            var cronJobs = new List<CronJob>();
            var entries = _jobRegistry.Build() ?? new CronJobRegistry.Entry[0];
            foreach (var entry in entries) {
                cronJobs.Add(new CronJob {
                    Name = entry.Name,
                    TypeName = entry.JobType.AssemblyQualifiedName,
                    Cron = entry.Cron,
                    LastRun = DateTime.MinValue
                });
            }
            return cronJobs.ToArray();
        }

        private ComputedCronJob[] Compute(IEnumerable<CronJob> jobs, CronJobRegistry.Entry[] entries)
            => jobs.Select(j => CreateComputedCronJob(j, entries)).ToArray();

        private ComputedCronJob CreateComputedCronJob(CronJob job, CronJobRegistry.Entry[] entries) {
            var entry = entries.First(e => e.Name == job.Name);
            return new ComputedCronJob(job, entry);
        }
    }
}
