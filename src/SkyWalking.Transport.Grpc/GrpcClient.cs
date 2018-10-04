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
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Config;
using SkyWalking.Logging;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Transport.Grpc
{
    public class GrpcClient : ISkyWalkingClient
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger _logger;
        private readonly GrpcConfig _config;

        public GrpcClient(ConnectionManager connectionManager, IConfigAccessor configAccessor, ILoggerFactory loggerFactory)
        {
            _connectionManager = connectionManager;
            _config = configAccessor.Get<GrpcConfig>();
            _logger = loggerFactory.CreateLogger(typeof(GrpcClient));
        }

        public async Task<NullableValue> RegisterApplicationAsync(string applicationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return NullableValue.Null;
            }

            var connection = _connectionManager.GetConnection();

            var client = new ApplicationRegisterService.ApplicationRegisterServiceClient(connection);

            return await ExecuteWithCatch(async () =>
                {
                    var applicationMapping = await client.applicationCodeRegisterAsync(new Application {ApplicationCode = applicationCode},
                        null, _config.GetTimeout(), cancellationToken);

                    return new NullableValue(applicationMapping?.Application?.Value ?? 0);
                },
                () => NullableValue.Null,
                () => ExceptionHelpers.RegisterApplicationError);
        }

        public async Task<NullableValue> RegisterApplicationInstanceAsync(int applicationId, Guid agentUUID, long registerTime, AgentOsInfoRequest osInfoRequest,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return NullableValue.Null;
            }

            var connection = _connectionManager.GetConnection();

            var client = new InstanceDiscoveryService.InstanceDiscoveryServiceClient(connection);

            var applicationInstance = new ApplicationInstance
            {
                ApplicationId = applicationId,
                AgentUUID = agentUUID.ToString("N"),
                RegisterTime = registerTime,
                Osinfo = new OSInfo
                {
                    OsName = osInfoRequest.OsName,
                    Hostname = osInfoRequest.HostName,
                    ProcessNo = osInfoRequest.ProcessNo
                }
            };

            applicationInstance.Osinfo.Ipv4S.AddRange(osInfoRequest.IpAddress);

            return await ExecuteWithCatch(async () =>
                {
                    var applicationInstanceMapping = await client.registerInstanceAsync(applicationInstance, null, _config.GetTimeout(), cancellationToken);
                    return new NullableValue(applicationInstanceMapping?.ApplicationInstanceId ?? 0);
                },
                () => NullableValue.Null,
                () => ExceptionHelpers.RegisterApplicationInstanceError);
        }

        public async Task HeartbeatAsync(int applicationInstance, long heartbeatTime, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return;
            }

            var connection = _connectionManager.GetConnection();

            var client = new InstanceDiscoveryService.InstanceDiscoveryServiceClient(connection);

            var heartbeat = new ApplicationInstanceHeartbeat
            {
                ApplicationInstanceId = applicationInstance,
                HeartbeatTime = heartbeatTime
            };
            await ExecuteWithCatch(async () => await client.heartbeatAsync(heartbeat, null, _config.GetTimeout(), cancellationToken), () => ExceptionHelpers.HeartbeatError);
        }

        public async Task CollectAsync(IEnumerable<TraceSegmentRequest> request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_connectionManager.Ready)
            {
                return;
            }

            var connection = _connectionManager.GetConnection();

            var client = new TraceSegmentService.TraceSegmentServiceClient(connection);
            try
            {
                using (var asyncClientStreamingCall = client.collect(null, null, cancellationToken))
                {
                    foreach (var segment in request)
                        await asyncClientStreamingCall.RequestStream.WriteAsync(TraceSegmentHelpers.Map(segment));
                    await asyncClientStreamingCall.RequestStream.CompleteAsync();
                    await asyncClientStreamingCall.ResponseAsync;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Heartbeat error.", ex);
                _connectionManager.Failure(ex);
            }
        }

        private async Task ExecuteWithCatch(Func<Task> task, Func<string> errMessage)
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                _logger.Error(errMessage(), ex);
                _connectionManager.Failure(ex);
            }
        }

        private async Task<T> ExecuteWithCatch<T>(Func<Task<T>> task, Func<T> errCallback, Func<string> errMessage)
        {
            try
            {
                return await task();
            }
            catch (Exception ex)
            {
                _logger.Error(errMessage(), ex);
                _connectionManager.Failure(ex);
                return errCallback();
            }
        }
    }
}