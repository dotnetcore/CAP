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
using SkyApm.Common;
using SkyApm.Tracing.Segments;

namespace SkyApm.Tracing
{
    public class SegmentContextFactory : ISegmentContextFactory
    {
        private readonly IEntrySegmentContextAccessor _entrySegmentContextAccessor;
        private readonly ILocalSegmentContextAccessor _localSegmentContextAccessor;
        private readonly IExitSegmentContextAccessor _exitSegmentContextAccessor;
        private readonly IRuntimeEnvironment _runtimeEnvironment;
        private readonly ISamplerChainBuilder _samplerChainBuilder;
        private readonly IUniqueIdGenerator _uniqueIdGenerator;

        public SegmentContextFactory(IRuntimeEnvironment runtimeEnvironment,
            ISamplerChainBuilder samplerChainBuilder,
            IUniqueIdGenerator uniqueIdGenerator,
            IEntrySegmentContextAccessor entrySegmentContextAccessor,
            ILocalSegmentContextAccessor localSegmentContextAccessor,
            IExitSegmentContextAccessor exitSegmentContextAccessor)
        {
            _runtimeEnvironment = runtimeEnvironment;
            _samplerChainBuilder = samplerChainBuilder;
            _uniqueIdGenerator = uniqueIdGenerator;
            _entrySegmentContextAccessor = entrySegmentContextAccessor;
            _localSegmentContextAccessor = localSegmentContextAccessor;
            _exitSegmentContextAccessor = exitSegmentContextAccessor;
        }

        public SegmentContext CreateEntrySegment(string operationName, ICarrier carrier)
        {
            var traceId = GetTraceId(carrier);
            var segmentId = GetSegmentId();
            var sampled = GetSampled(carrier, operationName);
            var segmentContext = new SegmentContext(traceId, segmentId, sampled, _runtimeEnvironment.ServiceId.Value,
                _runtimeEnvironment.ServiceInstanceId.Value, operationName, SpanType.Entry);

            if (carrier.HasValue)
            {
                var segmentReference = new SegmentReference
                {
                    Reference = Reference.CrossProcess,
                    EntryEndpoint = carrier.EntryEndpoint,
                    NetworkAddress = carrier.NetworkAddress,
                    ParentEndpoint = carrier.ParentEndpoint,
                    ParentSpanId = carrier.ParentSpanId,
                    ParentSegmentId = carrier.ParentSegmentId,
                    EntryServiceInstanceId = carrier.EntryServiceInstanceId,
                    ParentServiceInstanceId = carrier.ParentServiceInstanceId
                };
                segmentContext.References.Add(segmentReference);
            }

            _entrySegmentContextAccessor.Context = segmentContext;
            return segmentContext;
        }

        public SegmentContext CreateLocalSegment(string operationName)
        {
            var parentSegmentContext = GetParentSegmentContext(SpanType.Local);
            var traceId = GetTraceId(parentSegmentContext);
            var segmentId = GetSegmentId();
            var sampled = GetSampled(parentSegmentContext, operationName);
            var segmentContext = new SegmentContext(traceId, segmentId, sampled, _runtimeEnvironment.ServiceId.Value,
                _runtimeEnvironment.ServiceInstanceId.Value, operationName, SpanType.Local);

            if (parentSegmentContext != null)
            {
                var parentReference = parentSegmentContext.References.FirstOrDefault();
                var reference = new SegmentReference
                {
                    Reference = Reference.CrossThread,
                    EntryEndpoint = parentReference?.EntryEndpoint ?? parentSegmentContext.Span.OperationName,
                    NetworkAddress = parentReference?.NetworkAddress ?? parentSegmentContext.Span.OperationName,
                    ParentEndpoint = parentSegmentContext.Span.OperationName,
                    ParentSpanId = parentSegmentContext.Span.SpanId,
                    ParentSegmentId = parentSegmentContext.SegmentId,
                    EntryServiceInstanceId =
                        parentReference?.EntryServiceInstanceId ?? parentSegmentContext.ServiceInstanceId,
                    ParentServiceInstanceId = parentSegmentContext.ServiceInstanceId
                };
                segmentContext.References.Add(reference);
            }

            _localSegmentContextAccessor.Context = segmentContext;
            return segmentContext;
        }

        public SegmentContext CreateExitSegment(string operationName, StringOrIntValue networkAddress)
        {
            var parentSegmentContext = GetParentSegmentContext(SpanType.Exit);
            var traceId = GetTraceId(parentSegmentContext);
            var segmentId = GetSegmentId();
            var sampled = GetSampled(parentSegmentContext, operationName, networkAddress);
            var segmentContext = new SegmentContext(traceId, segmentId, sampled, _runtimeEnvironment.ServiceId.Value,
                _runtimeEnvironment.ServiceInstanceId.Value, operationName, SpanType.Exit);

            if (parentSegmentContext != null)
            {
                var parentReference = parentSegmentContext.References.FirstOrDefault();
                var reference = new SegmentReference
                {
                    Reference = Reference.CrossThread,
                    EntryEndpoint = parentReference?.EntryEndpoint ?? parentSegmentContext.Span.OperationName,
                    NetworkAddress = parentReference?.NetworkAddress ?? parentSegmentContext.Span.OperationName,
                    ParentEndpoint = parentSegmentContext.Span.OperationName,
                    ParentSpanId = parentSegmentContext.Span.SpanId,
                    ParentSegmentId = parentSegmentContext.SegmentId,
                    EntryServiceInstanceId =
                        parentReference?.EntryServiceInstanceId ?? parentSegmentContext.ServiceInstanceId,
                    ParentServiceInstanceId = parentSegmentContext.ServiceInstanceId
                };
                segmentContext.References.Add(reference);
            }

            segmentContext.Span.Peer = networkAddress;
            _exitSegmentContextAccessor.Context = segmentContext;
            return segmentContext;
        }

        public void Release(SegmentContext segmentContext)
        {
            segmentContext.Span.Finish();
            switch (segmentContext.Span.SpanType)
            {
                case SpanType.Entry:
                     _entrySegmentContextAccessor.Context = null;
                    break;
                case SpanType.Local:
                    _localSegmentContextAccessor.Context = null;
                    break;
                case SpanType.Exit:
                    _exitSegmentContextAccessor.Context = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SpanType), segmentContext.Span.SpanType, "Invalid SpanType.");
            }
        }

        private UniqueId GetTraceId(ICarrier carrier)
        {
            return carrier.HasValue ? carrier.TraceId : _uniqueIdGenerator.Generate();
        }

        private UniqueId GetTraceId(SegmentContext parentSegmentContext)
        {
            return parentSegmentContext?.TraceId ?? _uniqueIdGenerator.Generate();
        }

        private UniqueId GetSegmentId()
        {
            return _uniqueIdGenerator.Generate();
        }

        private bool GetSampled(ICarrier carrier, string operationName)
        {
            if (carrier.HasValue && carrier.Sampled.HasValue)
            {
                return carrier.Sampled.Value;
            }

            SamplingContext samplingContext;
            if (carrier.HasValue)
            {
                samplingContext = new SamplingContext(operationName, carrier.NetworkAddress, carrier.EntryEndpoint,
                    carrier.ParentEndpoint);
            }
            else
            {
                samplingContext = new SamplingContext(operationName, default(StringOrIntValue), default(StringOrIntValue),
                    default(StringOrIntValue));
            }

            var sampler = _samplerChainBuilder.Build();
            return sampler(samplingContext);
        }

        private bool GetSampled(SegmentContext parentSegmentContext, string operationName,
            StringOrIntValue peer = default(StringOrIntValue))
        {
            if (parentSegmentContext != null) return parentSegmentContext.Sampled;
            var sampledContext = new SamplingContext(operationName, peer, new StringOrIntValue(operationName),
                default(StringOrIntValue));
            var sampler = _samplerChainBuilder.Build();
            return sampler(sampledContext);
        }

        private SegmentContext GetParentSegmentContext(SpanType spanType)
        {
            switch (spanType)
            {
                case SpanType.Entry:
                    return null;
                case SpanType.Local:
                    return _entrySegmentContextAccessor.Context;
                case SpanType.Exit:
                    return _localSegmentContextAccessor.Context ?? _entrySegmentContextAccessor.Context;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spanType), spanType, "Invalid SpanType.");
            }
        }
    }
}