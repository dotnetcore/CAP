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

using System.Collections.Generic;
using SkyApm.Tracing;
using SkyApm.Tracing.Segments;

namespace SkyApm.Transport
{
    public class SegmentContextMapper : ISegmentContextMapper
    {
        public SegmentRequest Map(SegmentContext segmentContext)
        {
            var segmentRequest = new SegmentRequest
            {
                UniqueIds = new[]
                {
                    MapUniqueId(segmentContext.TraceId)
                }
            };
            var segmentObjectRequest = new SegmentObjectRequest
            {
                SegmentId = MapUniqueId(segmentContext.SegmentId),
                ServiceId = segmentContext.ServiceId,
                ServiceInstanceId = segmentContext.ServiceInstanceId
            };
            segmentRequest.Segment = segmentObjectRequest;
            var span = new SpanRequest
            {
                SpanId = segmentContext.Span.SpanId,
                ParentSpanId = segmentContext.Span.ParentSpanId,
                OperationName = segmentContext.Span.OperationName,
                StartTime = segmentContext.Span.StartTime,
                EndTime = segmentContext.Span.EndTime,
                SpanType = (int) segmentContext.Span.SpanType,
                SpanLayer = (int) segmentContext.Span.SpanLayer,
                IsError = segmentContext.Span.IsError,
                Peer = segmentContext.Span.Peer,
                Component = segmentContext.Span.Component
            };
            foreach (var reference in segmentContext.References)
                span.References.Add(new SegmentReferenceRequest
                {
                    ParentSegmentId = MapUniqueId(reference.ParentSegmentId),
                    ParentServiceInstanceId = reference.ParentServiceInstanceId,
                    ParentSpanId = reference.ParentSpanId,
                    ParentEndpointName = reference.ParentEndpoint,
                    EntryServiceInstanceId = reference.EntryServiceInstanceId,
                    EntryEndpointName = reference.EntryEndpoint,
                    NetworkAddress = reference.NetworkAddress,
                    RefType = (int) reference.Reference
                });

            foreach (var tag in segmentContext.Span.Tags)
                span.Tags.Add(new KeyValuePair<string, string>(tag.Key, tag.Value));

            foreach (var log in segmentContext.Span.Logs)
            {
                var logData = new LogDataRequest {Timestamp = log.Timestamp};
                foreach (var data in log.Data)
                    logData.Data.Add(new KeyValuePair<string, string>(data.Key, data.Value));
                span.Logs.Add(logData);
            }

            segmentObjectRequest.Spans.Add(span);
            return segmentRequest;
        }

        private static UniqueIdRequest MapUniqueId(UniqueId uniqueId)
        {
            return new UniqueIdRequest
            {
                Part1 = uniqueId.Part1,
                Part2 = uniqueId.Part2,
                Part3 = uniqueId.Part3
            };
        }
    }
}