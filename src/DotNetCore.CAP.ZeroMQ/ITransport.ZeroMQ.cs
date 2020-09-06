// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using NetMQ;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.CAP.ZeroMQ
{
    internal sealed class ZeroMQTransport : ITransport
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly ILogger _logger;
        private readonly string _exchange;

        public ZeroMQTransport(
            ILogger<ZeroMQTransport> logger,
            IConnectionChannelPool connectionChannelPool)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _exchange = _connectionChannelPool.Exchange;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("ZeroMQ", _connectionChannelPool.HostAddress);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            NetMQSocket channel = null;
            try
            {
                channel = _connectionChannelPool.Rent();
                NetMQMessage msg = new NetMQMessage();
                msg.Append(message.GetName());
                msg.Append(Newtonsoft.Json.JsonConvert.SerializeObject(message.Headers.ToDictionary(x => x.Key, x => (object)x.Value)));
                msg.Append(message.Body);
                channel.SendMultipartMessage(msg);
                _logger.LogDebug($"ZeroMQ topic message [{message.GetName()}] has been published.");
                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return Task.FromResult(OperateResult.Failed(wrapperEx, errors));
            }
            finally
            {
                if (channel != null)
                {
                    var returned = _connectionChannelPool.Return(channel);
                    if (!returned)
                    {
                        channel.Dispose();
                    }
                }
            }
        }
    }
}