// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class LoggerExtensions
    {
        public static void ConsumerExecutedAfterThreshold(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The Subscriber of the message({messageId}) still fails after {retries}th executions and we will stop retrying.");
        }

        public static void SenderAfterThreshold(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The Publisher of the message({messageId}) still fails after {retries}th sends and we will stop retrying.");
        }

        public static void ExecutedThresholdCallbackFailed(this ILogger logger, Exception ex)
        {
            logger.LogWarning(ex, "FailedThresholdCallback action raised an exception:" + ex.Message);
        }

        public static void ConsumerExecutionRetrying(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The {retries}th retrying consume a message failed. message id: {messageId}");
        }

        public static void SenderRetrying(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The {retries}th retrying send a message failed. message id: {messageId} ");
        }

        public static void MessageHasBeenSent(this ILogger logger, string name, string content)
        {
            logger.LogDebug($"Message published. name: {name}, content:{content}.");
        }

        public static void MessageReceived(this ILogger logger, string messageId, string name)
        {
            logger.LogDebug($"Received message. id:{messageId}, name: {name}");
        }

        public static void MessagePublishException(this ILogger logger, string messageId, string reason, Exception ex)
        {
            logger.LogError(ex, $"An exception occured while publishing a message, reason:{reason}. message id:{messageId}");
        }

        public static void ConsumerExecuted(this ILogger logger, double milliseconds)
        {
            logger.LogDebug($"Consumer executed. Took: {milliseconds} ms.");
        }

        public static void ServerStarting(this ILogger logger)
        {
            logger.LogInformation("Starting the processing server.");
        }

        public static void ProcessorsStartedError(this ILogger logger, Exception ex)
        {
            logger.LogError(ex, "Starting the processors throw an exception.");
        }

        public static void ServerShuttingDown(this ILogger logger)
        {
            logger.LogInformation("Shutting down the processing server...");
        }

        public static void ExpectedOperationCanceledException(this ILogger logger, Exception ex)
        {
            logger.LogWarning(ex, $"Expected an OperationCanceledException, but found '{ex.Message}'.");
        }

        public static void ModelBinderFormattingException(this ILogger logger, string methodName, string parameterName,
            string content, Exception ex)
        {
            logger.LogError(ex, $"When call subscribe method, a parameter format conversion exception occurs. MethodName:'{methodName}' ParameterName:'{parameterName}' Content:'{content}'.");
        }
    }
}