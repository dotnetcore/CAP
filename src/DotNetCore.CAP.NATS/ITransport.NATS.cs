// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using NATS.Client;

namespace DotNetCore.CAP.NATS
{
    internal class NATSTransport : ITransport
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;

        public NATSTransport(ILogger<NATSTransport> logger, IConnectionPool connectionPool)
        {
            _logger = logger;
            _connectionPool = connectionPool;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("NATS", _connectionPool.ServersAddress);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            var connection = _connectionPool.RentConnection();

            try
            {
                var msg = new Msg(message.GetName(), message.Body);
                foreach (var header in message.Headers)
                {
                    msg.Header[header.Key] = header.Value;
                }

                var reply = connection.Request(msg);

                if (reply.Data != null && reply.Data[0] == 1)
                {
                    _logger.LogDebug($"NATS subject message [{message.GetName()}] has been consumed.");

                    return Task.FromResult(OperateResult.Success);
                }
                throw new PublisherSentFailedException("NATS message send failed, no consumer reply!");
            }
            catch (Exception ex)
            {
                var warpEx = new PublisherSentFailedException(ex.Message, ex);

                return Task.FromResult(OperateResult.Failed(warpEx));
            }
            finally
            {
                _connectionPool.Return(connection);
            }
        }
    }
}