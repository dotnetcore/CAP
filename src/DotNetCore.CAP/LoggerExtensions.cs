using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, int, int, Exception> _serverStarting;
        private static readonly Action<ILogger, Exception> _serverStartingError;
        private static readonly Action<ILogger, Exception> _serverShuttingDown;
        private static readonly Action<ILogger, string, Exception> _expectedOperationCanceledException;

        private static readonly Action<ILogger, Exception> _cronJobsNotFound;
        private static readonly Action<ILogger, int, Exception> _cronJobsScheduling;
        private static readonly Action<ILogger, string, double, Exception> _cronJobExecuted;
        private static readonly Action<ILogger, string, Exception> _cronJobFailed;

        private static readonly Action<ILogger, string, string, Exception> _enqueuingSentMessage;
        private static readonly Action<ILogger, string, string, Exception> _enqueuingReceivdeMessage;
        private static readonly Action<ILogger, string, Exception> _executingConsumerMethod;
        private static readonly Action<ILogger, string, Exception> _receivedMessageRetryExecuting;

        static LoggerExtensions()
        {
            _serverStarting = LoggerMessage.Define<int, int>(
                LogLevel.Debug,
                1,
                "Starting the processing server. Detected {MachineProcessorCount} machine processor(s). Initiating {ProcessorCount} job processor(s).");

            _serverStartingError = LoggerMessage.Define(
                LogLevel.Error,
                5,
                "Starting the processing server throw an exception.");

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

            _enqueuingSentMessage = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "Enqueuing a topic to the sent message store. NameKey: {NameKey}. Content: {Content}");

            _enqueuingReceivdeMessage = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "Enqueuing a topic to the received message store. NameKey: {NameKey}. Content: {Content}");

            _executingConsumerMethod = LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Consumer method '{methodName}' failed to execute.");

            _receivedMessageRetryExecuting = LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Received message topic method '{topicName}' failed to execute.");
        }

        public static void ConsumerMethodExecutingFailed(this ILogger logger, string methodName, Exception ex)
        {
            _executingConsumerMethod(logger, methodName, ex);
        }

        public static void ReceivedMessageRetryExecutingFailed(this ILogger logger, string topicName, Exception ex)
        {
            _receivedMessageRetryExecuting(logger, topicName, ex);
        }

        public static void EnqueuingReceivedMessage(this ILogger logger, string nameKey, string content)
        {
            _enqueuingReceivdeMessage(logger, nameKey, content, null);
        }

        public static void EnqueuingSentMessage(this ILogger logger, string nameKey, string content)
        {
            _enqueuingSentMessage(logger, nameKey, content, null);
        }

        public static void ServerStarting(this ILogger logger, int machineProcessorCount, int processorCount)
        {
            _serverStarting(logger, machineProcessorCount, processorCount, null);
        }

        public static void ServerStartedError(this ILogger logger, Exception ex)
        {
            _serverStartingError(logger, ex);
        }

        public static void ServerShuttingDown(this ILogger logger)
        {
            _serverShuttingDown(logger, null);
        }

        public static void ExpectedOperationCanceledException(this ILogger logger, Exception ex)
        {
            _expectedOperationCanceledException(logger, ex.Message, ex);
        }

        public static void CronJobsNotFound(this ILogger logger)
        {
            _cronJobsNotFound(logger, null);
        }

        public static void CronJobsScheduling(this ILogger logger, IEnumerable<CronJob> jobs)
        {
            _cronJobsScheduling(logger, jobs.Count(), null);
        }

        public static void CronJobExecuted(this ILogger logger, string name, double seconds)
        {
            _cronJobExecuted(logger, name, seconds, null);
        }

        public static void CronJobFailed(this ILogger logger, string name, Exception ex)
        {
            _cronJobFailed(logger, name, ex);
        }
    }
}