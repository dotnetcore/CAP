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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkyWalking.Context.Trace;
using SkyWalking.Utils;

namespace SkyWalking.Context
{
    public class IgnoredTracerContext : ITracerContext
    {
        private static readonly NoopSpan noopSpan = new NoopSpan();
        private static readonly NoopEntrySpan noopEntrySpan=new NoopEntrySpan();

        private readonly Stack<ISpan> _spans = new Stack<ISpan>();
        
        public void Inject(IContextCarrier carrier)
        {
        }

        public void Extract(IContextCarrier carrier)
        {
        }

        public IContextSnapshot Capture { get; }

        public ISpan ActiveSpan
        {
            get
            {
                _spans.TryPeek(out var span);
                return span;
            }
        }

        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public void Continued(IContextSnapshot snapshot)
        {
        }

        public string GetReadableGlobalTraceId()
        {
            return string.Empty;
        }

        public ISpan CreateEntrySpan(string operationName)
        {
            _spans.Push(noopEntrySpan);
            return noopEntrySpan;
        }

        public ISpan CreateLocalSpan(string operationName)
        {
            _spans.Push(noopSpan);
            return noopSpan;
        }

        public ISpan CreateExitSpan(string operationName, string remotePeer)
        {
            var exitSpan = new NoopExitSpan(remotePeer);
            _spans.Push(exitSpan);
            return exitSpan;
        }

        public void StopSpan(ISpan span)
        {
            _spans.TryPop(out _);
            if (_spans.Count == 0)
            {
                ListenerManager.NotifyFinish(this);
                foreach (var item in Properties)
                {
                    if (item.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public static class ListenerManager
        {
            private static readonly List<IIgnoreTracerContextListener> _listeners = new List<IIgnoreTracerContextListener>();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Add(IIgnoreTracerContextListener listener)
            {
                _listeners.Add(listener);
            }

            public static void NotifyFinish(ITracerContext tracerContext)
            {
                foreach (var listener in _listeners)
                {
                    listener.AfterFinish(tracerContext);
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Remove(IIgnoreTracerContextListener listener)
            {
                _listeners.Remove(listener);
            }
        }
    }
}