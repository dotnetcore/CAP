// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    internal class CallbackMessageSender : ICallbackMessageSender
    {
        private readonly IContentSerializer _contentSerializer;
        private readonly ILogger<CallbackMessageSender> _logger;
        private readonly IMessagePacker _messagePacker;
        private readonly IServiceProvider _serviceProvider;

        public CallbackMessageSender(
            ILogger<CallbackMessageSender> logger,
            IServiceProvider serviceProvider,
            IContentSerializer contentSerializer,
            IMessagePacker messagePacker)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _contentSerializer = contentSerializer;
            _messagePacker = messagePacker;
        }

        public async Task SendAsync(string messageId, string topicName, object bodyObj)
        {
            string body;
            if (bodyObj != null && Helper.IsComplexType(bodyObj.GetType()))
            {
                body = _contentSerializer.Serialize(bodyObj);
            }
            else
            {
                body = bodyObj?.ToString();
            }

            _logger.LogDebug($"Callback message will publishing, name:{topicName},content:{body}");

            var callbackMessage = new CapMessageDto
            {
                Id = messageId,
                Content = body
            };

            var content = _messagePacker.Pack(callbackMessage);

            var publishedMessage = new CapPublishedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                Name = topicName,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            using (var scope = _serviceProvider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var callbackPublisher = provider.GetService<ICallbackPublisher>();
                await callbackPublisher.PublishCallbackAsync(publishedMessage);
            }
        }
    }
}