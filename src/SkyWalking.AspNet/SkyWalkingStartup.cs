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
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using SkyWalking.AspNet.Logging;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Logging;
using SkyWalking.Remote;

namespace SkyWalking.AspNet
{
    public class SkyWalkingStartup : IDisposable
    {
        public void Start()
        {
            LoadConfigurationSetting();
            LogManager.SetLoggerFactory(new DebugLoggerFactoryAdapter());
            AsyncContext.Run(async () => await StartAsync());
        }

        private async Task StartAsync()
        {
            await GrpcConnectionManager.Instance.ConnectAsync(TimeSpan.FromSeconds(3));
            await ServiceManager.Instance.Initialize();
        }

        private void LoadConfigurationSetting()
        {
            CollectorConfig.DirectServers = GetAppSetting(nameof(CollectorConfig.DirectServers), true);
            AgentConfig.ApplicationCode = GetAppSetting(nameof(AgentConfig.ApplicationCode), true);
            AgentConfig.Namespace = GetAppSetting(nameof(AgentConfig.Namespace), false);
            var samplePer3Secs = GetAppSetting(nameof(AgentConfig.SamplePer3Secs), false);
            if (int.TryParse(samplePer3Secs, out var v))
            {
                AgentConfig.SamplePer3Secs = v;
            }
            var pendingSegmentsLimit = GetAppSetting(nameof(AgentConfig.PendingSegmentsLimit), false);
            if(int.TryParse(pendingSegmentsLimit, out v))
            {
                AgentConfig.PendingSegmentsLimit = v;
            }
        }

        private string GetAppSetting(string key, bool @throw)
        {
            var value = WebConfigurationManager.AppSettings[key];
            if (@throw && string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Cannot read valid '{key}' in AppSettings.");
            }

            return value;
        }

        public void Dispose()
        {
            AsyncContext.Run(async () => await GrpcConnectionManager.Instance.ShutdownAsync());
            ServiceManager.Instance.Dispose();
        }
    }
}
