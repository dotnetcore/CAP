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
    public static class ListenerManager
    {
        private static readonly IList<ITracingContextListener> _listeners = new List<ITracingContextListener>();


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Add(ITracingContextListener listener)
        {
            _listeners.Add(listener);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Remove(ITracingContextListener listener)
        {
            _listeners.Remove(listener);
        }

        public static void NotifyFinish(ITraceSegment traceSegment)
        {
            foreach (var listener in _listeners)
            {
                listener.AfterFinished(traceSegment);
            }
        }
    }
}