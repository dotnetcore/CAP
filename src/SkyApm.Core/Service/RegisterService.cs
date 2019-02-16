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
    public class RegisterService : ExecutionService
    {
        private readonly InstrumentConfig _config;
        private readonly IServiceRegister _serviceRegister;
        private readonly TransportConfig _transportConfig;

        public RegisterService(IConfigAccessor configAccessor, IServiceRegister serviceRegister,
            IRuntimeEnvironment runtimeEnvironment, ILoggerFactory loggerFactory) : base(runtimeEnvironment,
            loggerFactory)
        {
            _serviceRegister = serviceRegister;
            _config = configAccessor.Get<InstrumentConfig>();
            _transportConfig = configAccessor.Get<TransportConfig>();
        }

        protected override TimeSpan DueTime { get; } = TimeSpan.Zero;

        protected override TimeSpan Period { get; } = TimeSpan.FromSeconds(30);

        protected override bool CanExecute() =>
            _transportConfig.ProtocolVersion == ProtocolVersions.V6 && !RuntimeEnvironment.Initialized;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await RegisterServiceAsync(cancellationToken);
            await RegisterServiceInstanceAsync(cancellationToken);
        }

        private async Task RegisterServiceAsync(CancellationToken cancellationToken)
        {
            if (!RuntimeEnvironment.ServiceId.HasValue)
            {
                var request = new ServiceRequest
                {
                    ServiceName = _config.ServiceName ?? _config.ApplicationCode
                };
                var value = await Polling(3,
                    () => _serviceRegister.RegisterServiceAsync(request, cancellationToken),
                    cancellationToken);
                if (value.HasValue && RuntimeEnvironment is RuntimeEnvironment environment)
                {
                    environment.ServiceId = value;
                    Logger.Information($"Registered Service[Id={environment.ServiceId.Value}].");
                }
            }
        }

        private async Task RegisterServiceInstanceAsync(CancellationToken cancellationToken)
        {
            if (RuntimeEnvironment.ServiceId.HasValue && !RuntimeEnvironment.ServiceInstanceId.HasValue)
            {
                var properties = new AgentOsInfoRequest
                {
                    HostName = DnsHelpers.GetHostName(),
                    IpAddress = DnsHelpers.GetIpV4s(),
                    OsName = PlatformInformation.GetOSName(),
                    ProcessNo = Process.GetCurrentProcess().Id,
                    Language = "dotnet"
                };
                var request = new ServiceInstanceRequest
                {
                    ServiceId = RuntimeEnvironment.ServiceId.Value,
                    InstanceUUID = RuntimeEnvironment.InstanceId.ToString("N"),
                    Properties = properties
                };
                var value = await Polling(3,
                    () => _serviceRegister.RegisterServiceInstanceAsync(request, cancellationToken),
                    cancellationToken);
                if (value.HasValue && RuntimeEnvironment is RuntimeEnvironment environment)
                {
                    environment.ServiceInstanceId = value;
                    Logger.Information($"Registered ServiceInstance[Id={environment.ServiceInstanceId.Value}].");
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
    }
}