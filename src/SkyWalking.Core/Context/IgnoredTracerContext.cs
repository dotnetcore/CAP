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


using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkyWalking.Context.Trace;

namespace SkyWalking.Context
{
    public class IgnoredTracerContext : ITracerContext
    {
        private static readonly NoopSpan noopSpan = new NoopSpan();

        private int _stackDepth;
        
        
        public void Inject(IContextCarrier carrier)
        {
        }

        public void Extract(IContextCarrier carrier)
        {
        }

        public IContextSnapshot Capture { get; }
        
        public ISpan ActiveSpan { get; }
        
        public void Continued(IContextSnapshot snapshot)
        {
        }

        public string GetReadableGlobalTraceId()
        {
            return string.Empty;
        }

        public ISpan CreateEntrySpan(string operationName)
        {
            _stackDepth++;
            return noopSpan;
        }

        public ISpan CreateLocalSpan(string operationName)
        {
            _stackDepth++;
            return noopSpan;
        }

        public ISpan CreateExitSpan(string operationName, string remotePeer)
        {
            _stackDepth++;
            return noopSpan;
        }

        public void StopSpan(ISpan span)
        {
            _stackDepth--;
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