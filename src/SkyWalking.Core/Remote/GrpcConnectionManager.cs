/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using SkyWalking.Config;
using SkyWalking.Logging;

namespace SkyWalking.Remote
{
    public class GrpcConnectionManager
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GrpcConnectionManager>();
        private static readonly GrpcConnectionManager _client = new GrpcConnectionManager();
        public const string NotFoundErrorMessage = "Not found available connection.";

        public static GrpcConnectionManager Instance => _client;

        private readonly Random _random = new Random();
        private readonly AsyncLock _lock = new AsyncLock();
        private GrpcConnection _connection;

        private GrpcConnectionManager()
        {
        }

        public async Task ConnectAsync()
        {
            // using async lock
            using (await _lock.LockAsync())
            {
                if (_connection != null && _connection.CheckState())
                {
                    return;
                }

                _connection = new GrpcConnection(GetServer(_connection?.Server));
                await _connection.ConnectAsync();
            }
        }

        public async Task ShutdownAsync()
        {
            await _connection?.ShutdowmAsync();
        }

        public GrpcConnection GetAvailableConnection()
        {
            var connection = _connection;
            if (connection == null || connection.State != GrpcConnectionState.Ready)
            {
                _logger.Debug(NotFoundErrorMessage);
                return null;
            }
            
            return connection;
        }

        private string GetServer(string currentServer)
        {
            var servers = RemoteDownstreamConfig.Collector.gRPCServers.Distinct().ToArray();
            if (servers.Length == 1)
            {
                return servers[0];
            }

            if (currentServer != null)
            {
                servers = servers.Where(x => x != currentServer).ToArray();
            }

            var index = _random.Next() % servers.Length;
            return servers[index];
        }
    }
}