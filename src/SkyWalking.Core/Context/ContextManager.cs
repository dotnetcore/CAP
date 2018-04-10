/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
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
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Context.Trace;
using SkyWalking.Dictionarys;
using SkyWalking.Sampling;

namespace SkyWalking.Context
{
    /// <summary>
    /// Context manager controls the whole context of tracing. Since .NET server application runs as same as Java,
    /// We also provide the CONTEXT propagation based on ThreadLocal mechanism.
    /// Meaning, each segment also related to singe thread.
    /// </summary>
    public class ContextManager :ITracingContextListener, IBootService
    {
        private static readonly ThreadLocal<ITracerContext> _context = new ThreadLocal<ITracerContext>();

        private static ITracerContext GetOrCreateContext(String operationName, bool forceSampling)
        {
            if (!_context.IsValueCreated)
            {
                if (string.IsNullOrEmpty(operationName))
                {
                    // logger.debug("No operation name, ignore this trace.");
                    _context.Value = new IgnoredTracerContext();
                }
                else
                {
                    if (!DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationId) &&
                        DictionaryUtil.IsNull(RemoteDownstreamConfig.Agent.ApplicationInstanceId))
                    {
                        var suffixIdx = operationName.LastIndexOf('.');
                        if (suffixIdx > -1 && AgentConfig.IgnoreSuffix.Contains(operationName.Substring(suffixIdx)))
                        {
                            _context.Value = new IgnoredTracerContext();
                        }
                        else
                        {
                            var sampler = ServiceManager.Instance.GetService<SamplingService>();
                            if (forceSampling || sampler.TrySampling())
                            {
                                _context.Value = new TracingContext();
                            }
                            else
                            {
                                _context.Value = new IgnoredTracerContext();
                            }
                        }
                    }
                    else
                    {
                        _context.Value = new IgnoredTracerContext();
                    }
                }

            }

            return _context.Value;
        }

        private static ITracerContext Context => _context.Value;

        public static string GlobalTraceId
        {
            get
            {
                if (_context.IsValueCreated)
                {
                    return _context.Value.GetReadableGlobalTraceId();
                }

                return "N/A";
            }
        }

        public static IContextSnapshot Capture => _context.Value?.Capture;

        public static ISpan CreateEntrySpan(string operationName, IContextCarrier carrier)
        {
            //todo samplingService
            return null;
        }

        public void AfterFinished(ITraceSegment traceSegment)
        {
        }

        public void Dispose()
        {
        }

        public void Init()
        {
            throw new NotImplementedException();
        }
    }
}
