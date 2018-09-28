/*
 * Licensed to the OpenSkywalking under one or more
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
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using CapEvents = DotNetCore.CAP.Diagnostics.CapDiagnosticListenerExtensions;


namespace SkyWalking.Diagnostics.CAP
{
    /// <summary>
    ///  Diagnostics processor for listen and process releted events of CAP.
    /// </summary>
    public class CapTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        private readonly IContextCarrierFactory _contextCarrierFactory;
        private Func<BrokerEventData, string> _brokerOperationNameResolver;

        public string ListenerName => CapEvents.DiagnosticListenerName;

        public Func<BrokerEventData, string> BrokerOperationNameResolver
        {
            get
            {
                return _brokerOperationNameResolver ??
                       (_brokerOperationNameResolver = (data) => "CAP " + data.BrokerTopicName);
            }
            set => _brokerOperationNameResolver = value ?? throw new ArgumentNullException(nameof(BrokerOperationNameResolver));
        }

        public CapTracingDiagnosticProcessor(IContextCarrierFactory contextCarrierFactory)
        {
            _contextCarrierFactory = contextCarrierFactory;
        }

        [DiagnosticName(CapEvents.CapBeforePublish)]
        public void CapBeforePublish([Object]BrokerPublishEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);
            var contextCarrier = _contextCarrierFactory.Create();
            var peer = eventData.BrokerAddress;
            var span = ContextManager.CreateExitSpan(operationName, contextCarrier, peer);
            span.SetComponent(ComponentsDefine.CAP);
            span.SetLayer(SpanLayer.MQ);
            Tags.MqTopic.Set(span, eventData.BrokerTopicName);
            foreach (var item in contextCarrier.Items)
            {
                eventData.Headers.Add(item.HeadKey, item.HeadValue);
            }
        }

        [DiagnosticName(CapEvents.CapAfterPublish)]
        public void CapAfterPublish([Object]BrokerPublishEndEventData eventData)
        {
            ContextManager.StopSpan();
        }

        [DiagnosticName(CapEvents.CapErrorPublish)]
        public void CapErrorPublish([Object]BrokerPublishErrorEventData eventData)
        {
            var capSpan = ContextManager.ActiveSpan;
            if (capSpan == null)
            {
                return;
            }
            capSpan.Log(eventData.Exception);
            capSpan.ErrorOccurred();
            ContextManager.StopSpan(capSpan);
        }

        [DiagnosticName(CapEvents.CapBeforeConsume)]
        public void CapBeforeConsume([Object]BrokerConsumeEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);
            var carrier = _contextCarrierFactory.Create();

            if (Helper.TryExtractTracingHeaders(eventData.BrokerTopicBody, out var headers,
                out var removedHeadersJson))
            {
                eventData.Headers = headers;
                eventData.BrokerTopicBody = removedHeadersJson;

                foreach (var tracingHeader in headers)
                {
                    foreach (var item in carrier.Items)
                    {
                        if (tracingHeader.Key == item.HeadKey)
                        {
                            item.HeadValue = tracingHeader.Value;
                        }
                    }
                }
            }
            var span = ContextManager.CreateEntrySpan(operationName, carrier);
            span.SetComponent(ComponentsDefine.CAP);
            span.SetLayer(SpanLayer.MQ);
            Tags.MqTopic.Set(span, eventData.BrokerTopicName);
        }

        [DiagnosticName(CapEvents.CapAfterConsume)]
        public void CapAfterConsume([Object]BrokerConsumeEndEventData eventData)
        {
            var capSpan = ContextManager.ActiveSpan;
            if (capSpan == null)
            {
                return;
            }

            ContextManager.StopSpan(capSpan);
        }

        [DiagnosticName(CapEvents.CapErrorConsume)]
        public void CapErrorConsume([Object]BrokerConsumeErrorEventData eventData)
        {
            var capSpan = ContextManager.ActiveSpan;
            if (capSpan == null)
            {
                return;
            }

            capSpan.Log(eventData.Exception);
            capSpan.ErrorOccurred();
            ContextManager.StopSpan(capSpan);
        }

        [DiagnosticName(CapEvents.CapBeforeSubscriberInvoke)]
        public void CapBeforeSubscriberInvoke([Object]SubscriberInvokeEventData eventData)
        {
            var span = ContextManager.CreateLocalSpan("Subscriber invoke");
            span.SetComponent(ComponentsDefine.CAP);
            span.Tag("subscriber.name", eventData.MethodName);
        }

        [DiagnosticName(CapEvents.CapAfterSubscriberInvoke)]
        public void CapAfterSubscriberInvoke([Object]SubscriberInvokeEventData eventData)
        {
            ContextManager.StopSpan();
        }

        [DiagnosticName(CapEvents.CapErrorSubscriberInvoke)]
        public void CapErrorSubscriberInvoke([Object]SubscriberInvokeErrorEventData eventData)
        {
            var capSpan = ContextManager.ActiveSpan;
            if (capSpan == null)
            {
                return;
            }

            capSpan.Log(eventData.Exception);
            capSpan.ErrorOccurred();
            ContextManager.StopSpan(capSpan);
        }
    }
}
