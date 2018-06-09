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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Context;
using SkyWalking.Dictionarys;
using SkyWalking.Logging;
using SkyWalking.NetworkProtocol;
using SkyWalking.Utils;

namespace SkyWalking.Remote
{
    public class GrpcApplicationService : TimerService
    {
        private static readonly ILogger _logger = LogManager.GetLogger<GrpcApplicationService>();
        public override int Order { get; } = -1;

        protected override TimeSpan Interval { get; } = TimeSpan.FromSeconds(15);

        protected override async Task Execute(CancellationToken token)
        {
            if (!DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationId) &&
                !DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationInstanceId))
            {
                return;
            }

            if (!GrpcConnectionManager.Instance.Available)
            {
                return;
            }

            var availableConnection = GrpcConnectionManager.Instance.GetAvailableConnection();

            if (availableConnection == null)
            {
                return;
            }

            try
            {
                await RegisterApplication(availableConnection, token);
                await RegisterApplicationInstance(availableConnection, token);
            }
            catch (Exception exception)
            {
                _logger.Warning($"Register application fail. {exception.Message}");
                availableConnection.Failure();
            }
        }

        private async Task RegisterApplication(GrpcConnection availableConnection, CancellationToken token)
        {
            if (DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationId))
            {
                var application = new Application {ApplicationCode = AgentConfig.ApplicationCode};
                var applicationRegisterService =
                    new ApplicationRegisterService.ApplicationRegisterServiceClient(availableConnection.GrpcChannel);
                
                var retry = 0;
                var applicationId = 0;
                while (retry++ < 3 && DictionaryUtil.IsNull(applicationId))
                {
                    var applicationMapping = await applicationRegisterService.applicationCodeRegisterAsync(application);
                    applicationId = applicationMapping?.Application?.Value ?? 0;
                    if (!DictionaryUtil.IsNull(applicationId))
                    {
                        break;
                    }
                    await Task.Delay(500, token);  
                }

                if (DictionaryUtil.IsNull(applicationId))
                {
                    _logger.Warning(
                        "Register application fail. Server response null.");
                    return;
                }

                _logger.Info(
                    $"Register application success. [applicationCode] = {application.ApplicationCode}. [applicationId] = {applicationId}");
                RemoteDownstreamConfig.Agent.ApplicationId = applicationId;
            }
        }

        private async Task RegisterApplicationInstance(GrpcConnection availableConnection, CancellationToken token)
        {
            if (!DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationId) && DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationInstanceId))
            {
                var instanceDiscoveryService =
                    new InstanceDiscoveryService.InstanceDiscoveryServiceClient(availableConnection.GrpcChannel);

                var agentUUID = Guid.NewGuid().ToString("N");
                var registerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var hostName = Dns.GetHostName();

                var osInfo = new OSInfo
                {
                    Hostname = hostName,
                    OsName = PlatformInformation.GetOSName(),
                    ProcessNo = Process.GetCurrentProcess().Id
                };

                osInfo.Ipv4S.AddRange(GetIpV4S(hostName));

                var applicationInstance = new ApplicationInstance
                {
                    ApplicationId = RemoteDownstreamConfig.Agent.ApplicationId,
                    AgentUUID = agentUUID,
                    RegisterTime = registerTime,
                    Osinfo = osInfo
                };

                var retry = 0;
                var applicationInstanceId = 0;
                while (retry++ < 5 && DictionaryUtil.IsNull(applicationInstanceId))
                {
                    var applicationInstanceMapping = await instanceDiscoveryService.registerInstanceAsync(applicationInstance);
                    applicationInstanceId = applicationInstanceMapping.ApplicationInstanceId;
                    if (!DictionaryUtil.IsNull(applicationInstanceId))
                    {
                        break;
                    }

                    await Task.Delay(500, token);
                }

                if (!DictionaryUtil.IsNull(applicationInstanceId))
                {
                    RemoteDownstreamConfig.Agent.ApplicationInstanceId = applicationInstanceId;
                    _logger.Info(
                        $"Register application instance success. [applicationInstanceId] = {applicationInstanceId}");
                }
                else
                {
                    _logger.Warning(
                        "Register application instance fail. Server response null.");
                }
            }
        }

        private IEnumerable<string> GetIpV4S(string hostName)
        {
            try
            {
                
                var ipAddresses = Dns.GetHostAddresses(hostName);
                var ipV4S = new List<string>();
                foreach (var ipAddress in ipAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork))
                    ipV4S.Add(ipAddress.ToString());
                return ipV4S;
            }
            catch (Exception e)
            {
                _logger.Warning($"Get host addresses fail. {e.Message}");
                return new string[0];
            }
        }
    }
}