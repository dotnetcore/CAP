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
using System.Linq;
using Google.Protobuf;
using SkyApm.Common;
using SkyWalking.NetworkProtocol;

namespace SkyApm.Transport.Grpc.Common
{
    internal static class SegmentV5Helpers
    {
        public static UpstreamSegment Map(SegmentRequest request)
        {
            var upstreamSegment = new UpstreamSegment();

            upstreamSegment.GlobalTraceIds.AddRange(request.UniqueIds.Select(MapToUniqueId).ToArray());

            var traceSegment = new TraceSegmentObject
            {
                TraceSegmentId = MapToUniqueId(request.Segment.SegmentId),
                ApplicationId = request.Segment.ServiceId,
                ApplicationInstanceId = request.Segment.ServiceInstanceId,
                IsSizeLimited = false
            };

            traceSegment.Spans.Add(request.Segment.Spans.Select(MapToSpan).ToArray());

            upstreamSegment.Segment = traceSegment.ToByteString();
            return upstreamSegment;
        }

        private static UniqueId MapToUniqueId(UniqueIdRequest uniqueIdRequest)
        {
            var uniqueId = new UniqueId();
            uniqueId.IdParts.Add(uniqueIdRequest.Part1);
            uniqueId.IdParts.Add(uniqueIdRequest.Part2);
            uniqueId.IdParts.Add(uniqueIdRequest.Part3);
            return uniqueId;
        }

        private static SpanObject MapToSpan(SpanRequest request)
        {
            var spanObject = new SpanObject
            {
                SpanId = request.SpanId,
                ParentSpanId = request.ParentSpanId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                SpanType = (SpanType) request.SpanType,
                SpanLayer = (SpanLayer) request.SpanLayer,
                IsError = request.IsError
            };

            ReadStringOrIntValue(spanObject, request.Component, ComponentReader, ComponentIdReader);
            ReadStringOrIntValue(spanObject, request.OperationName, OperationNameReader, OperationNameIdReader);
            ReadStringOrIntValue(spanObject, request.Peer, PeerReader, PeerIdReader);

            spanObject.Tags.Add(request.Tags.Select(x => new KeyWithStringValue {Key = x.Key, Value = x.Value}));
            spanObject.Refs.AddRange(request.References.Select(MapToSegmentReference).ToArray());
            spanObject.Logs.AddRange(request.Logs.Select(MapToLogMessage).ToArray());

            return spanObject;
        }

        private static TraceSegmentReference MapToSegmentReference(SegmentReferenceRequest referenceRequest)
        {
            var reference = new TraceSegmentReference
            {
                ParentApplicationInstanceId = referenceRequest.ParentServiceInstanceId,
                EntryApplicationInstanceId = referenceRequest.EntryServiceInstanceId,
                ParentSpanId = referenceRequest.ParentSpanId,
                RefType = (RefType) referenceRequest.RefType,
                ParentTraceSegmentId = MapToUniqueId(referenceRequest.ParentSegmentId)
            };

            ReadStringOrIntValue(reference, referenceRequest.NetworkAddress, NetworkAddressReader, NetworkAddressIdReader);
            ReadStringOrIntValue(reference, referenceRequest.EntryEndpointName, EntryServiceReader, EntryServiceIdReader);
            ReadStringOrIntValue(reference, referenceRequest.ParentEndpointName, ParentServiceReader, ParentServiceIdReader);

            return reference;
        }

        private static LogMessage MapToLogMessage(LogDataRequest request)
        {
            var logMessage = new LogMessage {Time = request.Timestamp};
            logMessage.Data.AddRange(request.Data.Select(x => new KeyWithStringValue {Key = x.Key, Value = x.Value}).ToArray());
            return logMessage;
        }

        private static void ReadStringOrIntValue<T>(T instance, StringOrIntValue stringOrIntValue, Action<T, string> stringValueReader, Action<T, int> intValueReader)
        {
            if (stringOrIntValue.HasStringValue)
            {
                stringValueReader.Invoke(instance, stringOrIntValue.GetStringValue());
            }
            else if (stringOrIntValue.HasIntValue)
            {
                intValueReader.Invoke(instance, stringOrIntValue.GetIntValue());
            }
        }

        private static readonly Action<SpanObject, string> ComponentReader = (s, val) => s.Component = val;
        private static readonly Action<SpanObject, int> ComponentIdReader = (s, val) => s.ComponentId = val;
        private static readonly Action<SpanObject, string> OperationNameReader = (s, val) => s.OperationName = val;
        private static readonly Action<SpanObject, int> OperationNameIdReader = (s, val) => s.OperationNameId = val;
        private static readonly Action<SpanObject, string> PeerReader = (s, val) => s.Peer = val;
        private static readonly Action<SpanObject, int> PeerIdReader = (s, val) => s.PeerId = val;
        private static readonly Action<TraceSegmentReference, string> NetworkAddressReader = (s, val) => s.NetworkAddress = val;
        private static readonly Action<TraceSegmentReference, int> NetworkAddressIdReader = (s, val) => s.NetworkAddressId = val;
        private static readonly Action<TraceSegmentReference, string> EntryServiceReader = (s, val) => s.EntryServiceName = val;
        private static readonly Action<TraceSegmentReference, int> EntryServiceIdReader = (s, val) => s.EntryServiceId = val;
        private static readonly Action<TraceSegmentReference, string> ParentServiceReader = (s, val) => s.ParentServiceName = val;
        private static readonly Action<TraceSegmentReference, int> ParentServiceIdReader = (s, val) => s.ParentServiceId = val;
    }
}