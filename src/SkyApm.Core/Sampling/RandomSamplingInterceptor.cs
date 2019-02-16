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
using SkyApm.Config;
using SkyApm.Tracing;

namespace SkyApm.Sampling
{
    public class RandomSamplingInterceptor : ISamplingInterceptor
    {
        private readonly Random _random;
        private readonly int _samplingRate;
        private readonly bool _sample_on;

        public RandomSamplingInterceptor(IConfigAccessor configAccessor)
        {
            var percentage = configAccessor.Get<SamplingConfig>().Percentage;
            _sample_on = percentage > 0;
            if (_sample_on)
            {
                _samplingRate = (int)(percentage * 100d);
            }
            _random = new Random();
        }

        public int Priority { get; } = int.MinValue + 1000;

        public bool Invoke(SamplingContext samplingContext, Sampler next)
        {
            if (!_sample_on) return next(samplingContext);
            var r = _random.Next(10000);
            return r <= _samplingRate && next(samplingContext);
        }
    }
}