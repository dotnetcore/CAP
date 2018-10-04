///*
// * Licensed to the OpenSkywalking under one or more
// * contributor license agreements.  See the NOTICE file distributed with
// * this work for additional information regarding copyright ownership.
// * The ASF licenses this file to You under the Apache License, Version 2.0
// * (the "License"); you may not use this file except in compliance with
// * the License.  You may obtain a copy of the License at
// *
// *     http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed under the License is distributed on an "AS IS" BASIS,
// * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// * See the License for the specific language governing permissions and
// * limitations under the License.
// *
// */
//
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using SkyWalking.Config;
//using SkyWalking.Logging;
//using SkyWalking.Utils;
//
//namespace SkyWalking.Remote
//{
//    public class GrpcConnectionManager
//    {
//        private static readonly ILogger Logger = LogManager.GetLogger<GrpcConnectionManager>();
//
//        public const string NotFoundErrorMessage = "Not found available connection.";
//
//        public static GrpcConnectionManager Instance { get; } = new GrpcConnectionManager();
//
//        private readonly Random _random = new Random();
//        private readonly AsyncLock _lock = new AsyncLock();
//        private GrpcConnection _connection;
//
//        public bool Available => _connection != null && _connection.CheckState();
//
//        private GrpcConnectionManager()
//        {
//        }
//
//        public async Task ConnectAsync(TimeSpan timeout)
//        {
//            // using async lock
//            using (await _lock.LockAsync())
//            {
//                if (_connection != null && _connection.CheckState())
//                {
//                    return;
//                }
//
//                if (_connection != null && !_connection.CheckState())
//                {
//                    await _connection.ShutdowmAsync();
//                }
//
//                var metadata = GetServerMetadata(_connection?.Server);
//                _connection = new GrpcConnection(metadata.Address, metadata.CertificatePath, metadata.Token);
//                await _connection.ConnectAsync(timeout);
//            }
//        }
//
//        public async Task ShutdownAsync()
//        {
//            await _connection?.ShutdowmAsync();
//        }
//
//        public GrpcConnection GetAvailableConnection()
//        {
//            var connection = _connection;
//            if (connection == null || connection.State != GrpcConnectionState.Ready)
//            {
//                Logger.Debug(NotFoundErrorMessage);
//                return null;
//            }
//
//            return connection;
//        }
//
//        private ServerMetadata GetServerMetadata(string currentServer)
//        {
//            return new ServerMetadata(GetServerAddress(currentServer),
//                CollectorConfig.CertificatePath, CollectorConfig.Authentication);
//        }
//
//        private string GetServerAddress(string currentServer)
//        {
//            var servers = RemoteDownstreamConfig.Collector.gRPCServers.Distinct().ToArray();
//            if (servers.Length == 1)
//            {
//                return servers[0];
//            }
//
//            if (currentServer != null)
//            {
//                servers = servers.Where(x => x != currentServer).ToArray();
//            }
//
//            var index = _random.Next() % servers.Length;
//            return servers[index];
//        }
//
//        public struct ServerMetadata
//        {
//            public string Address { get; }
//            
//            public string Token { get;  }
//            
//            public string CertificatePath { get; }
//
//            public ServerMetadata(string address, string certificate, string token)
//            {
//                Address = address;
//                CertificatePath = certificate;
//                Token = token;
//            }
//        } 
//    }
//}