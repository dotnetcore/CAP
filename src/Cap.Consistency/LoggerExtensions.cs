using System;
using System.Collections.Generic;
using System.Linq;
using Cap.Consistency.Job;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency
{
    internal static class LoggerExtensions
    {
        private static Action<ILogger, int, int, Exception> _serverStarting;
        private static Action<ILogger, Exception> _serverShuttingDown;
        private static Action<ILogger, string, Exception> _expectedOperationCanceledException;

        private static Action<ILogger, Exception> _cronJobsNotFound;
        private static Action<ILogger, int, Exception> _cronJobsScheduling;
        private static Action<ILogger, string, double, Exception> _cronJobExecuted;
        private static Action<ILogger, string, Exception> _cronJobFailed;

        private static Action<ILogger, Exception> _jobFailed;
        private static Action<ILogger, Exception> _jobFailedWillRetry;
        private static Action<ILogger, double, Exception> _jobExecuted;
        private static Action<ILogger, int, Exception> _jobRetrying;
        private static Action<ILogger, int, Exception> _jobCouldNotBeLoaded;
        private static Action<ILogger, int, Exception> _exceptionOccuredWhileExecutingJob;

        static LoggerExtensions() {
            _serverStarting = LoggerMessage.Define<int, int>(
                LogLevel.Debug,
                1,
                "Starting the processing server. Detected {MachineProcessorCount} machine processor(s). Initiating {ProcessorCount} job processor(s).");

            _serverShuttingDown = LoggerMessage.Define(
                LogLevel.Debug,
                2,
                "Shutting down the processing server...");

            _expectedOperationCanceledException = LoggerMessage.Define<string>(
                LogLevel.Warning,
                3,
                "Expected an OperationCanceledException, but found '{ExceptionMessage}'.");

            _cronJobsNotFound = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "No cron jobs found to schedule, cancelling processing of cron jobs.");

            _cronJobsScheduling = LoggerMessage.Define<int>(
                LogLevel.Debug,
                2,
                "Found {JobCount} cron job(s) to schedule.");

            _cronJobExecuted = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                3,
                "Cron job '{JobName}' executed succesfully. Took: {Seconds} secs.");

            _cronJobFailed = LoggerMessage.Define<string>(
                LogLevel.Warning,
                4,
                "Cron job '{jobName}' failed to execute.");

            _jobFailed = LoggerMessage.Define(
                LogLevel.Warning,
                1,
                "Job failed to execute.");

            _jobFailedWillRetry = LoggerMessage.Define(
                LogLevel.Warning,
                2,
                "Job failed to execute. Will retry.");

            _jobRetrying = LoggerMessage.Define<int>(
                LogLevel.Debug,
                3,
                "Retrying a job: {Retries}...");

            _jobExecuted = LoggerMessage.Define<double>(
                LogLevel.Debug,
                4,
                "Job executed. Took: {Seconds} secs.");

            _jobCouldNotBeLoaded = LoggerMessage.Define<int>(
                LogLevel.Warning,
                5,
                "Could not load a job: '{JobId}'.");

            _exceptionOccuredWhileExecutingJob = LoggerMessage.Define<int>(
                LogLevel.Error,
                6,
                "An exception occured while trying to execute a job: '{JobId}'. " +
                "Requeuing for another retry.");
        }

        public static void ServerStarting(this ILogger logger, int machineProcessorCount, int processorCount) {
            _serverStarting(logger, machineProcessorCount, processorCount, null);
        }

        public static void ServerShuttingDown(this ILogger logger) {
            _serverShuttingDown(logger, null);
        }

        public static void ExpectedOperationCanceledException(this ILogger logger, Exception ex) {
            _expectedOperationCanceledException(logger, ex.Message, ex);
        }

        public static void CronJobsNotFound(this ILogger logger) {
            _cronJobsNotFound(logger, null);
        }

        public static void CronJobsScheduling(this ILogger logger, IEnumerable<CronJob> jobs) {
            _cronJobsScheduling(logger, jobs.Count(), null);
        }

        public static void CronJobExecuted(this ILogger logger, string name, double seconds) {
            _cronJobExecuted(logger, name, seconds, null);
        }

        public static void CronJobFailed(this ILogger logger, string name, Exception ex) {
            _cronJobFailed(logger, name, ex);
        }

        public static void JobFailed(this ILogger logger, Exception ex) {
            _jobFailed(logger, ex);
        }

        public static void JobFailedWillRetry(this ILogger logger, Exception ex) {
            _jobFailedWillRetry(logger, ex);
        }

        public static void JobRetrying(this ILogger logger, int retries) {
            _jobRetrying(logger, retries, null);
        }

        public static void JobExecuted(this ILogger logger, double seconds) {
            _jobExecuted(logger, seconds, null);
        }

        public static void JobCouldNotBeLoaded(this ILogger logger, int jobId, Exception ex) {
            _jobCouldNotBeLoaded(logger, jobId, ex);
        }

        public static void ExceptionOccuredWhileExecutingJob(this ILogger logger, int jobId, Exception ex) {
            _exceptionOccuredWhileExecutingJob(logger, jobId, ex);
        }
    }
}