using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, int, int, Exception> _serverStarting;
        private static readonly Action<ILogger, Exception> _serverStartingError;
        private static readonly Action<ILogger, Exception> _serverShuttingDown;
        private static readonly Action<ILogger, string, Exception> _expectedOperationCanceledException;

        private static readonly Action<ILogger, string, string, Exception> _enqueuingSentMessage;
        private static readonly Action<ILogger, string, string, Exception> _enqueuingReceivdeMessage;
        private static readonly Action<ILogger, string, Exception> _executingConsumerMethod;
        private static readonly Action<ILogger, string, Exception> _receivedMessageRetryExecuting;
        private static readonly Action<ILogger, string, string, string, Exception> _modelBinderFormattingException;

        private static Action<ILogger, Exception> _jobFailed;
        private static Action<ILogger, Exception> _jobFailedWillRetry;
        private static Action<ILogger, double, Exception> _jobExecuted;
        private static Action<ILogger, int, Exception> _jobRetrying;
        private static Action<ILogger, string, Exception> _exceptionOccuredWhileExecutingJob;

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

            _enqueuingSentMessage = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "Enqueuing a topic to the sent message store. NameKey: '{NameKey}' Content: '{Content}'.");

            _enqueuingReceivdeMessage = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "Enqueuing a topic to the received message store. NameKey: '{NameKey}. Content: '{Content}'.");

            _executingConsumerMethod = LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Consumer method '{methodName}' failed to execute.");

            _receivedMessageRetryExecuting = LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Received message topic method '{topicName}' failed to execute.");

            _modelBinderFormattingException = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                5,
                "When call subscribe method, a parameter format conversion exception occurs. MethodName:'{MethodName}' ParameterName:'{ParameterName}' Content:'{Content}'."
                );

            _jobRetrying = LoggerMessage.Define<int>(
                LogLevel.Debug,
                3,
                "Retrying a job: {Retries}...");

            _jobExecuted = LoggerMessage.Define<double>(
                LogLevel.Debug,
                4,
                "Job executed. Took: {Seconds} secs.");

            _jobFailed = LoggerMessage.Define(
            LogLevel.Warning,
            1,
            "Job failed to execute.");

            _jobFailedWillRetry = LoggerMessage.Define(
                LogLevel.Warning,
                2,
                "Job failed to execute. Will retry.");

            _exceptionOccuredWhileExecutingJob = LoggerMessage.Define<string>(
              LogLevel.Error,
              6,
              "An exception occured while trying to execute a job: '{JobId}'. " +
              "Requeuing for another retry.");
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

        public static void ExceptionOccuredWhileExecutingJob(this ILogger logger, string jobId, Exception ex)
        {
            _exceptionOccuredWhileExecutingJob(logger, jobId, ex);
        }

        public static void ModelBinderFormattingException(this ILogger logger, string methodName, string parameterName, string content, Exception ex)
        {
            _modelBinderFormattingException(logger, methodName, parameterName, content, ex);
        }
    }
}