// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _serverStarting;
        private static readonly Action<ILogger, Exception> _processorsStartingError;
        private static readonly Action<ILogger, Exception> _serverShuttingDown;
        private static readonly Action<ILogger, string, Exception> _expectedOperationCanceledException;
        private static readonly Action<ILogger, string, string, string, Exception> _modelBinderFormattingException;
        private static readonly Action<ILogger, int, int, Exception> _consumerFailedWillRetry;
        private static readonly Action<ILogger, double, Exception> _consumerExecuted;
        private static readonly Action<ILogger, int, int, Exception> _senderRetrying;
        private static readonly Action<ILogger, string, Exception> _exceptionOccuredWhileExecuting;
        private static readonly Action<ILogger, double, Exception> _messageHasBeenSent;
        private static readonly Action<ILogger, int, string, Exception> _messagePublishException;

        static LoggerExtensions()
        {
            _serverStarting = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Starting the processing server.");

            _processorsStartingError = LoggerMessage.Define(
                LogLevel.Error,
                5,
                "Starting the processors throw an exception.");

            _serverShuttingDown = LoggerMessage.Define(
                LogLevel.Information,
                2,
                "Shutting down the processing server...");

            _expectedOperationCanceledException = LoggerMessage.Define<string>(
                LogLevel.Warning,
                3,
                "Expected an OperationCanceledException, but found '{ExceptionMessage}'.");

            LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Consumer method '{methodName}' failed to execute.");

            LoggerMessage.Define<string>(
                LogLevel.Error,
                5,
                "Received message topic method '{topicName}' failed to execute.");

            _modelBinderFormattingException = LoggerMessage.Define<string, string, string>(
                LogLevel.Error,
                5,
                "When call subscribe method, a parameter format conversion exception occurs. MethodName:'{MethodName}' ParameterName:'{ParameterName}' Content:'{Content}'."
            );

            _senderRetrying = LoggerMessage.Define<int, int>(
                LogLevel.Warning,
                3,
                "The {Retries}th retrying send a message failed. message id: {MessageId} ");

            _consumerExecuted = LoggerMessage.Define<double>(
                LogLevel.Debug,
                4,
                "Consumer executed. Took: {Seconds} secs.");

            _consumerFailedWillRetry = LoggerMessage.Define<int, int>(
                LogLevel.Warning,
                2,
                "The {Retries}th retrying consume a message failed. message id: {MessageId}");

            _exceptionOccuredWhileExecuting = LoggerMessage.Define<string>(
                LogLevel.Error,
                6,
                "An exception occured while trying to store a message. message id: {MessageId}");

            _messageHasBeenSent = LoggerMessage.Define<double>(
                LogLevel.Debug,
                4,
                "Message published. Took: {Seconds} secs.");

            _messagePublishException = LoggerMessage.Define<int, string>(
                LogLevel.Error,
                6,
                "An exception occured while publishing a message, reason:{Reason}. message id:{MessageId}");
        }

        public static void ConsumerExecutionRetrying(this ILogger logger, int messageId, int retries)
        {
            _consumerFailedWillRetry(logger, messageId, retries, null);
        }

        public static void SenderRetrying(this ILogger logger, int messageId, int retries)
        {
            _senderRetrying(logger, messageId, retries, null);
        }

        public static void MessageHasBeenSent(this ILogger logger, double seconds)
        {
            _messageHasBeenSent(logger, seconds, null);
        }

        public static void MessagePublishException(this ILogger logger, int messageId, string reason, Exception ex)
        {
            _messagePublishException(logger, messageId, reason, ex);
        }

        public static void ConsumerExecuted(this ILogger logger, double seconds)
        {
            _consumerExecuted(logger, seconds, null);
        }

        public static void ServerStarting(this ILogger logger)
        {
            _serverStarting(logger, null);
        }

        public static void ProcessorsStartedError(this ILogger logger, Exception ex)
        {
            _processorsStartingError(logger, ex);
        }

        public static void ServerShuttingDown(this ILogger logger)
        {
            _serverShuttingDown(logger, null);
        }

        public static void ExpectedOperationCanceledException(this ILogger logger, Exception ex)
        {
            _expectedOperationCanceledException(logger, ex.Message, ex);
        }

        public static void ExceptionOccuredWhileExecuting(this ILogger logger, string messageId, Exception ex)
        {
            _exceptionOccuredWhileExecuting(logger, messageId, ex);
        }

        public static void ModelBinderFormattingException(this ILogger logger, string methodName, string parameterName,
            string content, Exception ex)
        {
            _modelBinderFormattingException(logger, methodName, parameterName, content, ex);
        }
    }
}