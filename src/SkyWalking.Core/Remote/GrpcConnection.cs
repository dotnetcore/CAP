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
//using System.Threading.Tasks;
//using Grpc.Core;
//using SkyWalking.Logging;
//
//namespace SkyWalking.Remote
//{
//    public class GrpcConnection
//    {
//        private static readonly ILogger Logger = LogManager.GetLogger<GrpcConnection>();
//
//        public Channel GrpcChannel { get; }
//
//        public GrpcConnectionState State { get; private set; } = GrpcConnectionState.Idle;
//
//        public string Server { get; }
//
//        public GrpcConnection(string server, string rootCertificatePath = null, string token = null)
//        {
//            Server = server;
//            GrpcChannel = new GrpcChannelBuilder()
//                .WithServer(server)
//                .WithCredential(rootCertificatePath)
//                .WithAuthenticationToken(token)
//                .Build();
//        }
//
//        public async Task<bool> ConnectAsync(TimeSpan timeout)
//        {
//            if (State == GrpcConnectionState.Ready)
//            {
//                return true;
//            }
//            State = GrpcConnectionState.Connecting;
//            try
//            {
//                var deadLine = DateTime.UtcNow.AddSeconds(timeout.TotalSeconds);
//                await GrpcChannel.ConnectAsync(deadLine);
//                State = GrpcConnectionState.Ready;
//                Logger.Information($"Grpc channel connect success. [Server] = {GrpcChannel.Target}");
//            }
//            catch (TaskCanceledException ex)
//            {
//                State = GrpcConnectionState.Failure;
//                Logger.Warning($"Grpc channel connect timeout. {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                State = GrpcConnectionState.Failure;
//                Logger.Warning($"Grpc channel connect fail. {ex.Message}");
//            }
//
//            return State == GrpcConnectionState.Ready;
//        }
//
//        public async Task ShutdowmAsync()
//        {
//            try
//            {
//                await GrpcChannel.ShutdownAsync();
//            }
//            catch (Exception e)
//            {
//                Logger.Debug($"Grpc channel shutdown fail. {e.Message}");
//            }
//            finally
//            { 
//                State = GrpcConnectionState.Shutdown;
//            }
//        }
//
//        public bool CheckState()
//        {
//            return State == GrpcConnectionState.Ready && GrpcChannel.State == ChannelState.Ready;
//        }
//
//        public void Failure()
//        {
//            var currentState = State;
//            
//            if (GrpcConnectionState.Ready == currentState)
//            {
//                Logger.Debug($"Grpc channel state changed. {State} -> {GrpcChannel.State}");
//            }
//
//            State = GrpcConnectionState.Failure;
//        }
//    }
//}