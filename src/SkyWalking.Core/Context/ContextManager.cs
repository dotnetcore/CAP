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

using System.Collections.Generic;
using System.Threading;
using SkyWalking.Context.Trace;

namespace SkyWalking.Context
{
    /// <summary>
    /// Context manager controls the whole context of tracing. Since .NET server application runs as same as Java,
    /// We also provide the CONTEXT propagation based on ThreadLocal mechanism.
    /// Meaning, each segment also related to singe thread.
    /// </summary>
    public class ContextManager : ITracingContextListener, IIgnoreTracerContextListener
    {
        static ContextManager()
        {
            var manager = new ContextManager();
            TracingContext.ListenerManager.Add(manager);
            IgnoredTracerContext.ListenerManager.Add(manager);
        }

        private static readonly AsyncLocal<ITracerContext> _context = new AsyncLocal<ITracerContext>();

        private static ITracerContext GetOrCreateContext(string operationName, bool forceSampling)
        {
            var context = _context.Value;
            if (context == null)
            {
                if (string.IsNullOrEmpty(operationName))
                {
                    // logger.debug("No operation name, ignore this trace.");
                    _context.Value = new IgnoredTracerContext();
                }
                else
                {
                    if (RuntimeEnvironment.Instance.Initialized)
                    {
//                        var suffixIdx = operationName.LastIndexOf('.');
//                        if (suffixIdx > -1 && AgentConfig.IgnoreSuffix.Contains(operationName.Substring(suffixIdx)))
//                        {
//                            _context.Value = new IgnoredTracerContext();
//                        }
//                        else
//                        {
                            var sampler = DefaultSampler.Instance;
                            if (forceSampling || sampler.Sampled())
                            {
                                _context.Value = new TracingContext();
                            }
                            else
                            {
                                _context.Value = new IgnoredTracerContext();
                            }
//                        }
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
                if (_context.Value != null)
                {
                    return _context.Value.GetReadableGlobalTraceId();
                }

                return "N/A";
            }
        }

        public static IContextSnapshot Capture => _context.Value?.Capture;

        public static IDictionary<string, object> ContextProperties => _context.Value?.Properties;

        public static ISpan CreateEntrySpan(string operationName, IContextCarrier carrier)
        {
            var samplingService = DefaultSampler.Instance;
            if (carrier != null && carrier.IsValid)
            {
                samplingService.ForceSampled();
                var context = GetOrCreateContext(operationName, true);
                var span = context.CreateEntrySpan(operationName);
                context.Extract(carrier);
                return span;
            }
            else
            {
                var context = GetOrCreateContext(operationName, false);

                return context.CreateEntrySpan(operationName);
            }
        }

        public static ISpan CreateLocalSpan(string operationName)
        {
            var context = GetOrCreateContext(operationName, false);
            return context.CreateLocalSpan(operationName);
        }

        public static ISpan CreateExitSpan(string operationName, IContextCarrier carrier, string remotePeer)
        {
            var context = GetOrCreateContext(operationName, false);
            var span = context.CreateExitSpan(operationName, remotePeer);
            context.Inject(carrier);
            return span;
        }

        public static ISpan CreateExitSpan(string operationName, string remotePeer)
        {
            var context = GetOrCreateContext(operationName, false);
            var span = context.CreateExitSpan(operationName, remotePeer);
            return span;
        }

        public static void Inject(IContextCarrier carrier)
        {
            Context?.Inject(carrier);
        }

        public static void Extract(IContextCarrier carrier)
        {
            Context?.Extract(carrier);
        }

        public static void Continued(IContextSnapshot snapshot)
        {
            if (snapshot.IsValid && !snapshot.IsFromCurrent)
            {
                Context?.Continued(snapshot);
            }
        }

        public static void StopSpan()
        {
            StopSpan(ActiveSpan);
        }

        public static ISpan ActiveSpan
        {
            get { return Context?.ActiveSpan; }
        }

        public static ITracerContext ActiveContext
        {
            get
            {
                return Context;
            }
        }

        public static void StopSpan(ISpan span, ITracerContext context=null)
        {
            if (Context != null)
            {
                Context.StopSpan(span);
            }
            else if (context != null)
            {
                context.StopSpan(span);
            }
        }

        public void AfterFinished(ITraceSegment traceSegment)
        {
            _context.Value = null;
        }

        public void AfterFinish(ITracerContext tracerContext)
        {
            _context.Value = null;
        }
    }
}