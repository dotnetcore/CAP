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

namespace SkyWalking.Service
{
    public class SamplingRefreshService : ExecutionService
    {
        private readonly SamplingConfig _config;

        public SamplingRefreshService(IConfigAccessor configAccessor,
            IRuntimeEnvironment runtimeEnvironment, ILoggerFactory loggerFactory)
            : base(runtimeEnvironment, loggerFactory)
        {
            _config = configAccessor.Get<SamplingConfig>();
            DefaultSampler.Instance.SetSamplePer3Secs(_config.SamplePer3Secs);
        }

        protected override TimeSpan DueTime { get; } = TimeSpan.Zero;

        protected override TimeSpan Period { get; } = TimeSpan.FromSeconds(3);

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            DefaultSampler.Instance.Reset();
            return Task.CompletedTask;
        }
    }
}