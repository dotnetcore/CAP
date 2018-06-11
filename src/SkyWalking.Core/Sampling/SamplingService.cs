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
using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Utils;

namespace SkyWalking.Sampling
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SamplingService :TimerService, ISampler
    {
        private readonly AtomicInteger _atomicInteger = new AtomicInteger();
        private readonly int _sample_N_Per_3_Secs = AgentConfig.Sample_N_Per_3_Secs;
        private readonly bool _sample_on = AgentConfig.Sample_N_Per_3_Secs > 0;

        public bool TrySampling()
        {
            if (!_sample_on)
            {
                return true;
            }

            return _atomicInteger.Increment() < _sample_N_Per_3_Secs;
        }

        public void ForceSampled()
        {
            if (_sample_on)
            {
                _atomicInteger.Increment();
            }
        }

        protected override TimeSpan Interval { get; } = TimeSpan.FromSeconds(3);
        
        protected override Task Execute(CancellationToken token)
        {
            if (_sample_on)
            {
                _atomicInteger.Value = 0;
            }

            return Task.CompletedTask;
        }
    }
}