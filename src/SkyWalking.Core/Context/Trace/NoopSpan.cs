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
using System.Collections.Generic;
using System.Text;
using SkyWalking.NetworkProtocol.Trace;

namespace SkyWalking.Context.Trace
{
    public class NoopSpan : ISpan
    {
        public int SpanId => 0;

        public string OperationName { get => string.Empty; set { } }

        public int OperationId { get => 0; set { } }

        public ISpan ErrorOccurred()
        {
            return this;
        }

        public ISpan Log(Exception exception)
        {
            return this;
        }

        public ISpan Log(long timestamp, IDictionary<string, object> @event)
        {
            return this;
        }

        public void Ref(ITraceSegmentRef traceSegmentRef)
        {
        }

        public ISpan SetComponent(IComponent component)
        {
            return this;
        }

        public ISpan SetComponent(string componentName)
        {
            return this;
        }

        public ISpan SetLayer(SpanLayer layer)
        {
            return this;
        }

        public ISpan Start()
        {
            return this;
        }

        public ISpan Start(long timestamp)
        {
            return this;
        }

        public ISpan Tag(string key, string value)
        {
            return this;
        }


        public virtual bool IsEntry => false;

        public virtual bool IsExit => false;
    }
}
