/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The OpenSkywalking licenses this file to You under the Apache License, Version 2.0
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
using System.Linq;
using SkyWalking.Transport;
using SkyWalking.Context.Ids;

namespace SkyWalking.Context.Trace
{
    public class TraceSegment : ITraceSegment
    {
        private readonly IList<ITraceSegmentRef> _refs;
        private readonly IList<AbstractTracingSpan> _spans;
        private readonly DistributedTraceIdCollection _relatedGlobalTraces;
        private bool _isSizeLimited;

        public int ApplicationId => RuntimeEnvironment.Instance.ServiceId.Value;

        public int ApplicationInstanceId => RuntimeEnvironment.Instance.ServiceInstanceId.Value;

        public IEnumerable<ITraceSegmentRef> Refs => _refs;

        public IEnumerable<DistributedTraceId> RelatedGlobalTraces => _relatedGlobalTraces.GetRelatedGlobalTraces();

        public ID TraceSegmentId { get; }

        public bool HasRef => _refs.Count > 0;

        public bool IsIgnore { get; set; }

        public bool IsSingleSpanSegment => _spans.Count == 1;

        public TraceSegment()
        {
            TraceSegmentId = GlobalIdGenerator.Generate();
            _spans = new List<AbstractTracingSpan>();
            _relatedGlobalTraces = new DistributedTraceIdCollection();
            _relatedGlobalTraces.Append(new NewDistributedTraceId());
            _refs = new List<ITraceSegmentRef>();
        }

        public void Archive(AbstractTracingSpan finishedSpan)
        {
            _spans.Add(finishedSpan);
        }

        public ITraceSegment Finish(bool isSizeLimited)
        {
            _isSizeLimited = isSizeLimited;
            return this;
        }

        /// <summary>
        /// Establish the link between this segment and its parents.
        /// </summary>
        public void Ref(ITraceSegmentRef refSegment)
        {
            if (!_refs.Contains(refSegment))
            {
                _refs.Add(refSegment);
            }
        }

        public void RelatedGlobalTrace(DistributedTraceId distributedTraceId)
        {
            _relatedGlobalTraces.Append(distributedTraceId);
        }

        public SegmentRequest Transform()
        {
            var upstreamSegment = new SegmentRequest
            {
                UniqueIds = _relatedGlobalTraces.GetRelatedGlobalTraces()
                    .Select(x => x.ToUniqueId()).ToArray()
            };

            upstreamSegment.Segment = new SegmentObjectRequest
            {
                SegmentId = TraceSegmentId.Transform(),
                Spans = _spans.Select(x => x.Transform()).ToArray(),
                ServiceId = ApplicationId,
                ServiceInstanceId = ApplicationInstanceId
            };
            
            return upstreamSegment;
        }

        public override string ToString()
        {
            return "TraceSegment{"
                   +
                   $"traceSegmentId='{TraceSegmentId}', refs={_refs}, spans={_spans}, relatedGlobalTraces={_relatedGlobalTraces}"
                   + "}";
        }
    }
}