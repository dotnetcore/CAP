// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using NATS.Client;
using NATS.Client.JetStream;

namespace DotNetCore.CAP.NATS
{
    internal class NATSTransport : ITransport
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;
        private readonly JetStreamOptions _jetStreamOptions;

        public NATSTransport(ILogger<NATSTransport> logger, IConnectionPool connectionPool)
        {
            _logger = logger;
            _connectionPool = connectionPool;

            _jetStreamOptions = JetStreamOptions.Builder().WithPublishNoAck(false).WithRequestTimeout(3000).Build();
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("NATS", _connectionPool.ServersAddress);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            var connection = _connectionPool.RentConnection();

            try
            {
                var msg = new Msg(message.GetName(), message.Body);
                foreach (var header in message.Headers)
                {
                    msg.Header[header.Key] = header.Value;
                }

                var js = connection.CreateJetStreamContext(_jetStreamOptions);

                var builder = PublishOptions.Builder().WithMessageId(message.GetId());

                var resp = await js.PublishAsync(msg, builder.Build());

                if (resp.Seq > 0)
                {
                    _logger.LogDebug($"NATS stream message [{message.GetName()}] has been published.");

                    return OperateResult.Success;
                }
                
                throw new PublisherSentFailedException("NATS message send failed, no consumer reply!");
            }
            catch (Exception ex)
            {
                var warpEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(warpEx);
            }
            finally
            {
                _connectionPool.Return(connection);
            }
        }
    }
}