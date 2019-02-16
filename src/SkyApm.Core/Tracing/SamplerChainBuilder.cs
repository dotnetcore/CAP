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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SkyApm.Tracing
{
    public class SamplerChainBuilder : ISamplerChainBuilder
    {
        private volatile int state = 0;
        private readonly IEnumerable<ISamplingInterceptor> _sampledInterceptors;
        private Sampler _sampler;

        public SamplerChainBuilder(IEnumerable<ISamplingInterceptor> sampledInterceptors)
        {
            _sampledInterceptors = sampledInterceptors;
        }

        public Sampler Build()
        {
            if (_sampler != null)
                return _sampler;

            if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
            {
                var samplers = _sampledInterceptors.OrderBy(x => x.Priority).Select(interceptor =>
                    (Func<Sampler, Sampler>) (next => ctx => interceptor.Invoke(ctx, next))).ToList();

                Sampler sampler = ctx => true;
                foreach (var next in samplers)
                {
                    sampler = next(sampler);
                }

                return _sampler = sampler;
            }

            while (_sampler == null)
            {
            }

            return _sampler;
        }
    }
}