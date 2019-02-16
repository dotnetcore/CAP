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
using SkyApm.Common;
using SkyApm.Tracing.Segments;
using SkyApm.Transport;

namespace SkyApm.Tracing
{
    public class TracingContext : ITracingContext
    {
        private readonly ISegmentContextFactory _segmentContextFactory;
        private readonly ICarrierPropagator _carrierPropagator;
        private readonly ISegmentDispatcher _segmentDispatcher;

        public TracingContext(ISegmentContextFactory segmentContextFactory, ICarrierPropagator carrierPropagator,
            ISegmentDispatcher segmentDispatcher)
        {
            _segmentContextFactory = segmentContextFactory;
            _carrierPropagator = carrierPropagator;
            _segmentDispatcher = segmentDispatcher;
        }

        public SegmentContext CreateEntrySegmentContext(string operationName, ICarrierHeaderCollection carrierHeader)
        {
            if (operationName == null) throw new ArgumentNullException(nameof(operationName));
            var carrier = _carrierPropagator.Extract(carrierHeader);
            return _segmentContextFactory.CreateEntrySegment(operationName, carrier);
        }

        public SegmentContext CreateLocalSegmentContext(string operationName)
        {
            if (operationName == null) throw new ArgumentNullException(nameof(operationName));
            return _segmentContextFactory.CreateLocalSegment(operationName);
        }

        public SegmentContext CreateExitSegmentContext(string operationName, string networkAddress,
            ICarrierHeaderCollection carrierHeader = default(ICarrierHeaderCollection))
        {
            var segmentContext =
                _segmentContextFactory.CreateExitSegment(operationName, new StringOrIntValue(networkAddress));
            if (carrierHeader != null)
                _carrierPropagator.Inject(segmentContext, carrierHeader);
            return segmentContext;
        }

        public void Release(SegmentContext segmentContext)
        {
            if (segmentContext == null)
            {
                return;
            }
            
            _segmentContextFactory.Release(segmentContext);
            if (segmentContext.Sampled)
                _segmentDispatcher.Dispatch(segmentContext);
        }
    }
}