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

using SkyApm.Common;

namespace SkyApm.Tracing
{
    public class Carrier : ICarrier
    {
        public bool HasValue { get; } = true;
        
        public bool? Sampled { get; set; }
        
        public UniqueId TraceId { get; }
        
        public UniqueId ParentSegmentId { get; }
        
        public int ParentSpanId { get; }
        
        public int ParentServiceInstanceId { get; }
        
        public int EntryServiceInstanceId { get; }
        
        public StringOrIntValue NetworkAddress { get; set; }
        
        public StringOrIntValue EntryEndpoint { get; set; }
        
        public StringOrIntValue ParentEndpoint { get; set; }

        public Carrier(UniqueId traceId, UniqueId parentSegmentId, int parentSpanId, int parentServiceInstanceId,
            int entryServiceInstanceId)
        {
            TraceId = traceId;
            ParentSegmentId = parentSegmentId;
            ParentSpanId = parentSpanId;
            ParentServiceInstanceId = parentServiceInstanceId;
            EntryServiceInstanceId = entryServiceInstanceId;
        }
    }
}