// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
    public abstract class BasePublishMessageSender : IPublishMessageSender, IPublishExecutor
    {
        private readonly IStorageConnection _connection;
        private readonly ILogger _logger;
        private readonly CapOptions _options;
        private readonly IStateChanger _stateChanger;

        // diagnostics listener
        // ReSharper disable once InconsistentNaming
        protected static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        protected BasePublishMessageSender(
            ILogger logger,
            CapOptions options,
            IStorageConnection connection,
            IStateChanger stateChanger)
        {
            _options = options;
            _connection = connection;
            _stateChanger = stateChanger;
            _logger = logger;
        }

        public abstract Task<OperateResult> PublishAsync(string keyName, string content);

        public async Task<OperateResult> SendAsync(CapPublishedMessage message)
        {
            var sp = Stopwatch.StartNew();

            var result = await PublishAsync(message.Name, message.Content);

            sp.Stop();

            if (result.Succeeded)
            {
                await SetSuccessfulState(message);

                _logger.MessageHasBeenSent(sp.Elapsed.TotalSeconds);

                return OperateResult.Success;
            }
            else
            {
                _logger.MessagePublishException(message.Id, result.Exception);

                await SetFailedState(message, result.Exception, out bool stillRetry);
                if (stillRetry)
                {
                    _logger.SenderRetrying(3);

                    await SendAsync(message);
                }
                return OperateResult.Failed(result.Exception);
            }
        }

        private static bool UpdateMessageForRetryAsync(CapPublishedMessage message)
        {
            var retryBehavior = RetryBehavior.DefaultRetry;

            var retries = ++message.Retries;
            if (retries >= retryBehavior.RetryCount)
            {
                return false;
            }

            var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
            message.ExpiresAt = due;

            return true;
        }

        private Task SetSuccessfulState(CapPublishedMessage message)
        {
            var succeededState = new SucceededState(_options.SucceedMessageExpiredAfter);

            return _stateChanger.ChangeStateAsync(message, succeededState, _connection);
        }

        private Task SetFailedState(CapPublishedMessage message, Exception ex, out bool stillRetry)
        {
            IState newState = new FailedState();

            if (ex is PublisherSentFailedException)
            {
                stillRetry = false;
                message.Retries = _options.FailedRetryCount; // not retry if PublisherSentFailedException
            }
            else
            {
                stillRetry = UpdateMessageForRetryAsync(message);
                if (stillRetry)
                {
                    _logger.ConsumerExecutionFailedWillRetry(ex);
                    return Task.CompletedTask;
                }
            }

            AddErrorReasonToContent(message, ex);

            return _stateChanger.ChangeStateAsync(message, newState, _connection);
        }

        private static void AddErrorReasonToContent(CapPublishedMessage message, Exception exception)
        {
            message.Content = Helper.AddExceptionProperty(message.Content, exception);
        }
    }
}