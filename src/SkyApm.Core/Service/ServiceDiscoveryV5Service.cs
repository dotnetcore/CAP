/*
 * Licensed to the SkyAPM under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The SkyAPM licenses this file to You under the Apache License, Version 2.0
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
using System.Threading;
using System.Threading.Tasks;
using SkyApm.Common;
using SkyApm.Config;
using SkyApm.Logging;
using SkyApm.Transport;

namespace SkyApm.Service
{
    public class ServiceDiscoveryV5Service : ExecutionService
    {
        private readonly InstrumentConfig _config;
        private readonly TransportConfig _transportConfig;
        private readonly ISkyApmClientV5 skyApmClient;

        protected override TimeSpan DueTime { get; } = TimeSpan.Zero;

        protected override TimeSpan Period { get; } = TimeSpan.FromSeconds(30);

        public ServiceDiscoveryV5Service(IConfigAccessor configAccessor, ISkyApmClientV5 skyApmClient,
            IRuntimeEnvironment runtimeEnvironment, ILoggerFactory loggerFactory)
            : base(runtimeEnvironment, loggerFactory)
        {
            _config = configAccessor.Get<InstrumentConfig>();
            _transportConfig = configAccessor.Get<TransportConfig>();
            this.skyApmClient = skyApmClient;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await RegisterApplication(cancellationToken);
            await RegisterApplicationInstance(cancellationToken);
            await Heartbeat(cancellationToken);
        }

        protected override bool CanExecute() =>
            _transportConfig.ProtocolVersion == ProtocolVersions.V5 && !RuntimeEnvironment.Initialized;

        private async Task RegisterApplication(CancellationToken cancellationToken)
        {
            if (!RuntimeEnvironment.ServiceId.HasValue)
            {
                var value = await Polling(3,
                    () => skyApmClient.RegisterApplicationAsync(_config.ServiceName ?? _config.ApplicationCode, cancellationToken),
                    cancellationToken);
                if (value.HasValue && RuntimeEnvironment is RuntimeEnvironment environment)
                {
                    environment.ServiceId = value;
                    Logger.Information($"Registered Application[Id={environment.ServiceId.Value}].");
                }
            }
        }

        private async Task RegisterApplicationInstance(CancellationToken cancellationToken)
        {
            if (RuntimeEnvironment.ServiceId.HasValue && !RuntimeEnvironment.ServiceInstanceId.HasValue)
            {
                var osInfoRequest = new AgentOsInfoRequest
                {
                    HostName = DnsHelpers.GetHostName(),
                    IpAddress = DnsHelpers.GetIpV4s(),
                    OsName = PlatformInformation.GetOSName(),
                    ProcessNo = Process.GetCurrentProcess().Id
                };
                var value = await Polling(3,
                    () => skyApmClient.RegisterApplicationInstanceAsync(RuntimeEnvironment.ServiceId.Value,
                        RuntimeEnvironment.InstanceId,
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), osInfoRequest, cancellationToken),
                    cancellationToken);
                if (value.HasValue && RuntimeEnvironment is RuntimeEnvironment environment)
                {
                    environment.ServiceInstanceId = value;
                    Logger.Information(
                        $"Registered Application Instance[Id={environment.ServiceInstanceId.Value}].");
                }
            }
        }

        private static async Task<NullableValue> Polling(int retry, Func<Task<NullableValue>> execute,
            CancellationToken cancellationToken)
        {
            var index = 0;
            while (index++ < retry)
            {
                var value = await execute();
                if (value.HasValue)
                {
                    return value;
                }

                await Task.Delay(500, cancellationToken);
            }

            return NullableValue.Null;
        }

        private async Task Heartbeat(CancellationToken cancellationToken)
        {
            if (RuntimeEnvironment.Initialized)
            {
                try
                {
                    await skyApmClient.HeartbeatAsync(RuntimeEnvironment.ServiceInstanceId.Value,
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken);
                    Logger.Debug($"Heartbeat at {DateTimeOffset.UtcNow}.");
                }
                catch (Exception e)
                {
                    Logger.Error("Heartbeat error.", e);
                }
            }
        }
    }
}