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
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using SkyApm.Tracing;
using CapEvents = DotNetCore.CAP.Diagnostics.CapDiagnosticListenerExtensions;

namespace SkyApm.Diagnostics.CAP
{
    /// <summary>
    ///  Diagnostics processor for listen and process releted events of CAP.
    /// </summary>
    public class CapTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        private Func<BrokerEventData, string> _brokerOperationNameResolver;
        public string ListenerName => CapEvents.DiagnosticListenerName;

        public Func<BrokerEventData, string> BrokerOperationNameResolver
        {
            get
            {
                return _brokerOperationNameResolver ??
                       (_brokerOperationNameResolver = (data) => "CAP " + data.BrokerTopicName);
            }
            set => _brokerOperationNameResolver =
                value ?? throw new ArgumentNullException(nameof(BrokerOperationNameResolver));
        }

        private readonly ITracingContext _tracingContext;
        private readonly IEntrySegmentContextAccessor _entrySegmentContextAccessor;
        private readonly IExitSegmentContextAccessor _exitSegmentContextAccessor;

        public CapTracingDiagnosticProcessor(ITracingContext tracingContext,
            IEntrySegmentContextAccessor entrySegmentContextAccessor,
            IExitSegmentContextAccessor exitSegmentContextAccessor)
        {
            _tracingContext = tracingContext;
            _exitSegmentContextAccessor = exitSegmentContextAccessor;
            _entrySegmentContextAccessor = entrySegmentContextAccessor;
        }

        [DiagnosticName(CapEvents.CapBeforePublish)]
        public void CapBeforePublish([Object] BrokerPublishEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);
            var context = _tracingContext.CreateExitSegmentContext(operationName, eventData.BrokerAddress,
                new CapCarrierHeaderCollection(eventData.Headers));
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.MQ;
            context.Span.Component = Common.Components.CAP;
            context.Span.AddTag(Common.Tags.MQ_TOPIC, eventData.BrokerTopicName);
        }

        [DiagnosticName(CapEvents.CapAfterPublish)]
        public void CapAfterPublish([Object] BrokerPublishEndEventData eventData)
        {
            _tracingContext.Release(_exitSegmentContextAccessor.Context);
        }

        [DiagnosticName(CapEvents.CapErrorPublish)]
        public void CapErrorPublish([Object] BrokerPublishErrorEventData eventData)
        {
            var context = _exitSegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }

        [DiagnosticName(CapEvents.CapBeforeConsume)]
        public void CapBeforeConsume([Object] BrokerConsumeEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);

            ICarrierHeaderCollection carrierHeader = null;
            if (Helper.TryExtractTracingHeaders(eventData.BrokerTopicBody, out var headers,
                out var removedHeadersJson))
            {
                eventData.Headers = headers;
                eventData.BrokerTopicBody = removedHeadersJson;
                carrierHeader = new CapCarrierHeaderCollection(headers);
            }

            var context = _tracingContext.CreateEntrySegmentContext(operationName, carrierHeader);
            context.Span.SpanLayer = Tracing.Segments.SpanLayer.MQ;
            context.Span.Component = Common.Components.CAP;
            context.Span.AddTag(Common.Tags.MQ_TOPIC, eventData.BrokerTopicName);
        }

        [DiagnosticName(CapEvents.CapAfterConsume)]
        public void CapAfterConsume([Object] BrokerConsumeEndEventData eventData)
        {
            _tracingContext.Release(_entrySegmentContextAccessor.Context);
        }

        [DiagnosticName(CapEvents.CapErrorConsume)]
        public void CapErrorConsume([Object] BrokerConsumeErrorEventData eventData)
        {
            var context = _entrySegmentContextAccessor.Context;
            if (context != null)
            {
                context.Span.ErrorOccurred(eventData.Exception);
                _tracingContext.Release(context);
            }
        }

        [DiagnosticName(CapEvents.CapBeforeSubscriberInvoke)]
        public void CapBeforeSubscriberInvoke([Object] SubscriberInvokeEventData eventData)
        {
        }

        [DiagnosticName(CapEvents.CapAfterSubscriberInvoke)]
        public void CapAfterSubscriberInvoke([Object] SubscriberInvokeEventData eventData)
        {
        }

        [DiagnosticName(CapEvents.CapErrorSubscriberInvoke)]
        public void CapErrorSubscriberInvoke([Object] SubscriberInvokeErrorEventData eventData)
        {
        }
    }
}