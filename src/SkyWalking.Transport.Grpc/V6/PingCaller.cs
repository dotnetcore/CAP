/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Config;
using SkyWalking.Logging;
using SkyWalking.NetworkProtocol;
using SkyWalking.Transport.Grpc.Common;

namespace SkyWalking.Transport.Grpc.V6
{
    public class PingCaller : IPingCaller
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger _logger;
        private readonly GrpcConfig _config;

        public PingCaller(ConnectionManager connectionManager, ILoggerFactory loggerFactory,
            IConfigAccessor configAccessor)
        {
            _connectionManager = connectionManager;
            _config = configAccessor.Get<GrpcConfig>();
            _logger = loggerFactory.CreateLogger(typeof(PingCaller));
        }

        public Task PingAsync(PingRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return Task.CompletedTask;
            }

            var connection = _connectionManager.GetConnection();
            return new Call(_logger, _connectionManager).Execute(async () =>
                {
                    var client = new ServiceInstancePing.ServiceInstancePingClient(connection);
                    await client.doPingAsync(new ServiceInstancePingPkg
                    {
                        ServiceInstanceId = request.ServiceInstanceId,
                        ServiceInstanceUUID = request.InstanceId,
                        Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, null, _config.GetTimeout(), cancellationToken);
                },
                () => ExceptionHelpers.PingError);
        }
    }
}