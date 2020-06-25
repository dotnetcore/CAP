// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

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
                var binFormatter = new BinaryFormatter();
                using var mStream = new MemoryStream();
                binFormatter.Serialize(mStream, message);

                connection.Publish(message.GetName(), mStream.ToArray());

                _logger.LogDebug($"kafka topic message [{message.GetName()}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var warpEx = new PublisherSentFailedException(ex.Message, ex);

                return Task.FromResult(OperateResult.Failed(warpEx));
            }
            finally
            {
                var returned = _connectionPool.Return(connection);
                if (!returned)
                {
                    connection.Dispose();
                }
            }
        }
    }
}