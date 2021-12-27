// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Pulsar
{
    internal class PulsarTransport : ITransport
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public PulsarTransport(ILogger<PulsarTransport> logger, IConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public BrokerAddress BrokerAddress => new ("Pulsar", _connectionFactory.ServersAddress);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            var producer = await _connectionFactory.CreateProducerAsync(message.GetName());

            try
            {
                var headerDic = new Dictionary<string, string?>(message.Headers);
                headerDic.TryGetValue(PulsarHeaders.PulsarKey, out var key);
                var pulsarMessage = producer.NewMessage(message.Body!, key, headerDic);
                var result = await producer.SendAsync(pulsarMessage);

                if (result.Type != null)
                {
                    _logger.LogDebug($"pulsar topic message [{message.GetName()}] has been published.");

                    return OperateResult.Success;
                }

                throw new PublisherSentFailedException("pulsar message persisted failed!");
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }
    }
}