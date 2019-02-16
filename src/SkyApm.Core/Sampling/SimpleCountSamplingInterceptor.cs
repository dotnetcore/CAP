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
using System.Threading;
using System.Threading.Tasks;
using SkyApm.Common;
using SkyApm.Config;
using SkyApm.Logging;
using SkyApm.Tracing;

namespace SkyApm.Sampling
{
    public class SimpleCountSamplingInterceptor : ExecutionService, ISamplingInterceptor
    {
        private readonly bool _sample_on;
        private readonly int _samplePer3Secs;
        private readonly AtomicInteger _idx = new AtomicInteger();

        public SimpleCountSamplingInterceptor(IConfigAccessor configAccessor,IRuntimeEnvironment runtimeEnvironment, ILoggerFactory loggerFactory) :
            base(runtimeEnvironment, loggerFactory)
        {
            var samplingConfig = configAccessor.Get<SamplingConfig>();
            _samplePer3Secs = samplingConfig.SamplePer3Secs;
            _sample_on = _samplePer3Secs > -1;
        }

        public int Priority { get; } = int.MinValue + 999;

        public bool Invoke(SamplingContext samplingContext, Sampler next)
        {
            if (!_sample_on) return next(samplingContext);
            return _idx.Increment() <= _samplePer3Secs && next(samplingContext);
        }

        protected override TimeSpan DueTime { get; } = TimeSpan.Zero;

        protected override TimeSpan Period { get; } = TimeSpan.FromSeconds(3);

        protected override bool CanExecute() => _sample_on && base.CanExecute();

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Reset();
            return Task.CompletedTask;
        }
        
        private void Reset()
        {
            _idx.Value = 0;
        }
    }
}