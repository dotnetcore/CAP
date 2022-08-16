// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
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

        public static void ConsumerDuplicates(this ILogger logger, string subscriber, string group)
        {
            logger.LogWarning($"We detected that you have duplicate subscribers ({subscriber}) in same group ({group}), this will cause diversity behavior.");
        }

        public static void ConsumerExecutionRetrying(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The {retries}th retrying consume a message failed. message id: {messageId}");
        }

        public static void SenderRetrying(this ILogger logger, string messageId, int retries)
        {
            logger.LogWarning($"The {retries}th retrying send a message failed. message id: {messageId} ");
        }

        public static void MessageReceived(this ILogger logger, string messageId, string name)
        {
            logger.LogDebug($"Received message. id:{messageId}, name: {name}");
        }

        public static void MessagePublishException(this ILogger logger, string? messageId, string reason, Exception? ex)
        {
            logger.LogError(ex, $"An exception occurred while publishing a message, reason:{reason}. message id:{messageId}");
        }

        public static void ConsumerExecuting(this ILogger logger, string className, string methodName, string group)
        {
            logger.LogInformation($"Executing subscriber method '{className}.{methodName}' on group '{group}'");
        }

        public static void ConsumerExecuted(this ILogger logger, string className, string methodName, string group, double milliseconds, string? instance)
        {
            logger.LogInformation($"Executed subscriber method '{className}.{methodName}' on group '{group}' with instance '{instance}' in {milliseconds}ms");
        }

        public static void ConsumerExecuteFailed(this ILogger logger, string topic, string id, string? instance, Exception? ex)
        {
            logger.LogError(ex, $"An exception occurred while executing the subscription method. Topic:{topic}, Id:{id}, Instance: {instance}");
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
    }
}