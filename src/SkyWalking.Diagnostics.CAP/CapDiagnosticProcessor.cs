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
using Microsoft.Extensions.DiagnosticAdapter;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using SkyWalking.NetworkProtocol.Trace;
using CapEvents = DotNetCore.CAP.Diagnostics.CapDiagnosticListenerExtensions;


namespace SkyWalking.Diagnostics.CAP
{
    /// <summary>
    ///  Diagnostics processor for listen and process releted events of CAP.
    /// </summary>
    public class CapDiagnosticProcessor : ITracingDiagnosticProcessor
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
            set => _brokerOperationNameResolver = value ?? throw new ArgumentNullException(nameof(BrokerOperationNameResolver));
        }

        [DiagnosticName(CapEvents.CapBeforePublish)]
        public void CapBeforePublish(BrokerPublishEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);
            var contextCarrier = new ContextCarrier();
            var peer = eventData.BrokerAddress;
            var span = ContextManager.CreateExitSpan(operationName, contextCarrier, peer);
            span.SetComponent(ComponentsDefine.CAP);
            span.SetLayer(SpanLayer.MQ);
            span.Tag(Tags.MqTopic.Key, eventData.BrokerTopicName);
            foreach (var item in contextCarrier.Items)
            {
                eventData.Headers.Add(item.HeadKey, item.HeadValue);
            }
        }

        [DiagnosticName(CapEvents.CapAfterPublish)]
        public void CapAfterPublish(BrokerPublishEndEventData eventData)
        {
            ContextManager.StopSpan();
        }

        [DiagnosticName(CapEvents.CapErrorPublish)]
        public void CapErrorPublish(BrokerPublishErrorEventData eventData)
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
        public void CapBeforeConsume(BrokerConsumeEventData eventData)
        {
            var operationName = BrokerOperationNameResolver(eventData);
            var carrier = new ContextCarrier();

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
            span.Tag(Tags.MqTopic.Key, eventData.BrokerTopicName);
        }

        [DiagnosticName(CapEvents.CapAfterConsume)]
        public void CapAfterConsume(BrokerConsumeEndEventData eventData)
        {
            var capSpan = ContextManager.ActiveSpan;
            if (capSpan == null)
            {
                return;
            }

            ContextManager.StopSpan(capSpan);
        }

        [DiagnosticName(CapEvents.CapErrorConsume)]
        public void CapErrorConsume(BrokerConsumeErrorEventData eventData)
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
        public void CapBeforeSubscriberInvoke(SubscriberInvokeEventData eventData)
        {
            var span = ContextManager.CreateLocalSpan("Subscriber invoke");
            span.SetComponent(ComponentsDefine.CAP);
            span.Tag("subscriber.name", eventData.MethodName);
        }

        [DiagnosticName(CapEvents.CapAfterSubscriberInvoke)]
        public void CapAfterSubscriberInvoke(SubscriberInvokeEventData eventData)
        {
            ContextManager.StopSpan();
        }

        [DiagnosticName(CapEvents.CapErrorSubscriberInvoke)]
        public void CapErrorSubscriberInvoke(SubscriberInvokeErrorEventData eventData)
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
