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

using System.Collections;
using System.Collections.Generic;
using SkyWalking.Context.Trace;
using SkyWalking.Sampling;

namespace SkyWalking.Context
{
    public class TracingContext: ITracerContext
    {
        private long _lastWarningTimestamp = 0;
        private ISampler _sampler;
        private ITraceSegment _segment;
        private Stack<ISpan> _activeSpanStacks;
        private int _spanIdGenerator;

        public TracingContext()
        {
            _sampler = new SamplingService();
            _segment = new TraceSegment();
            _activeSpanStacks = new Stack<ISpan>();
        }

        public void Inject(IContextCarrier carrier)
        {
            throw new System.NotImplementedException();
        }

        public void Extract(IContextCarrier carrier)
        {
            throw new System.NotImplementedException();
        }

        public IContextSnapshot Capture { get; }
        
        public ISpan ActiveSpan { get; }
        
        public void Continued(IContextSnapshot snapshot)
        {
            throw new System.NotImplementedException();
        }

        public string GetReadableGlobalTraceId()
        {
            throw new System.NotImplementedException();
        }

        public ISpan CreateEntrySpan(string operationName)
        {
            throw new System.NotImplementedException();
        }

        public ISpan CreateLocalSpan(string operationName)
        {
            throw new System.NotImplementedException();
        }

        public ISpan CreateExitSpan(string operationName, string remotePeer)
        {
            throw new System.NotImplementedException();
        }

        public void StopSpan(ISpan span)
        {
            throw new System.NotImplementedException();
        }
    }
}