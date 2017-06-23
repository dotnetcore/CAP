using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Kafka
{
    internal static class LoggerExtensions
    {
        private static Action<ILogger, Exception> _collectingExpiredEntities;

        private static Action<ILogger, Exception> _installing;
        private static Action<ILogger, Exception> _installingError;
        private static Action<ILogger, Exception> _installingSuccess;

        private static Action<ILogger, Exception> _jobFailed;
        private static Action<ILogger, Exception> _jobFailedWillRetry;
        private static Action<ILogger, double, Exception> _jobExecuted;
        private static Action<ILogger, int, Exception> _jobRetrying;
        private static Action<ILogger, int, Exception> _jobCouldNotBeLoaded;
        private static Action<ILogger, string, Exception> _exceptionOccuredWhileExecutingJob;

        static LoggerExtensions()
        {
            _collectingExpiredEntities = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Collecting expired entities.");

            _installing = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Installing Jobs SQL objects...");

            _installingError = LoggerMessage.Define(
                LogLevel.Warning,
                2,
                "Exception occurred during automatic migration. Retrying...");

            _installingSuccess = LoggerMessage.Define(
                LogLevel.Debug,
                3,
                "Jobs SQL objects installed.");

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

            _exceptionOccuredWhileExecutingJob = LoggerMessage.Define<string>(
                LogLevel.Error,
                6,
                "An exception occured while trying to execute a job: '{JobId}'. " +
                "Requeuing for another retry.");
        }

        public static void CollectingExpiredEntities(this ILogger logger)
        {
            _collectingExpiredEntities(logger, null);
        }

        public static void Installing(this ILogger logger)
        {
            _installing(logger, null);
        }

        public static void InstallingError(this ILogger logger, Exception ex)
        {
            _installingError(logger, ex);
        }

        public static void InstallingSuccess(this ILogger logger)
        {
            _installingSuccess(logger, null);
        }

        public static void JobFailed(this ILogger logger, Exception ex)
        {
            _jobFailed(logger, ex);
        }

        public static void JobFailedWillRetry(this ILogger logger, Exception ex)
        {
            _jobFailedWillRetry(logger, ex);
        }

        public static void JobRetrying(this ILogger logger, int retries)
        {
            _jobRetrying(logger, retries, null);
        }

        public static void JobExecuted(this ILogger logger, double seconds)
        {
            _jobExecuted(logger, seconds, null);
        }

        public static void JobCouldNotBeLoaded(this ILogger logger, int jobId, Exception ex)
        {
            _jobCouldNotBeLoaded(logger, jobId, ex);
        }

        public static void ExceptionOccuredWhileExecutingJob(this ILogger logger, string jobId, Exception ex)
        {
            _exceptionOccuredWhileExecutingJob(logger, jobId, ex);
        }
    }
}