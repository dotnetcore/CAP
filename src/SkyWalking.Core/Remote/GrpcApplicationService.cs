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
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Context;
using SkyWalking.Dictionarys;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Remote
{
    public class GrpcApplicationService : TimerService
    {
        public override int Order { get; } = -1;

        protected override async Task Initializing(CancellationToken token)
        {
            var application = new Application {ApplicationCode = AgentConfig.ApplicationCode};
            var applicationRegisterService =
                new ApplicationRegisterService.ApplicationRegisterServiceClient(GrpcChannelManager.Instance.Channel);

            var applicationId = default(int?);

            while (!applicationId.HasValue || DictionaryUtil.IsNull(applicationId.Value))
            {
                var applicationMapping = await applicationRegisterService.applicationCodeRegisterAsync(application);
                applicationId = applicationMapping?.Application?.Value;
            }

            RemoteDownstreamConfig.Agent.ApplicationId = applicationId.Value;

            var instanceDiscoveryService =
                new InstanceDiscoveryService.InstanceDiscoveryServiceClient(GrpcChannelManager.Instance.Channel);

            var agentUUID = Guid.NewGuid().ToString().Replace("-", "");
            var registerTime = DateTime.UtcNow.GetTimeMillis();

            var hostName = Dns.GetHostName();
           
            var osInfo = new OSInfo
            {
                Hostname = hostName,
                OsName = Environment.OSVersion.ToString(),
                ProcessNo = Process.GetCurrentProcess().Id
            };

            // todo fix Device not configured
            //var ipv4s = Dns.GetHostAddresses(hostName);          
            //foreach (var ipAddress in ipv4s.Where(x => x.AddressFamily == AddressFamily.InterNetwork))
            //   osInfo.Ipv4S.Add(ipAddress.ToString());

            var applicationInstance = new ApplicationInstance
            {
                ApplicationId = applicationId.Value,
                AgentUUID = agentUUID,
                RegisterTime = registerTime,
                Osinfo = osInfo
            };

            var applicationInstanceId = 0;
           
            while (DictionaryUtil.IsNull(applicationInstanceId))
            {
                var applicationInstanceMapping = await instanceDiscoveryService.registerInstanceAsync(applicationInstance);
                applicationInstanceId = applicationInstanceMapping.ApplicationInstanceId;
            }

            RemoteDownstreamConfig.Agent.ApplicationInstanceId = applicationInstanceId;

        }

        protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);
           
        protected override async Task Execute(CancellationToken token)
        {
            var instanceDiscoveryService =
                new InstanceDiscoveryService.InstanceDiscoveryServiceClient(GrpcChannelManager.Instance.Channel);

            var heartbeat = new ApplicationInstanceHeartbeat
            {
                ApplicationInstanceId = RemoteDownstreamConfig.Agent.ApplicationInstanceId,
                HeartbeatTime = DateTime.UtcNow.GetTimeMillis()
            };
            
            await instanceDiscoveryService.heartbeatAsync(heartbeat);
        }
    }
}