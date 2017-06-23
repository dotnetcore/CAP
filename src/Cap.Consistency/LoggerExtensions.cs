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

    }
}