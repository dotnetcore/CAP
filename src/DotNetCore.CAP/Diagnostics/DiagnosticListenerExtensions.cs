// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Diagnostics
{
    /// <summary>
    /// Extension methods on the DiagnosticListener class to log CAP data
    /// </summary>
    public static class CapDiagnosticListenerExtensions
    {
        public const string DiagnosticListenerName = "CapDiagnosticListener";

        private const string CapPrefix = "DotNetCore.CAP.";

        public const string CapBeforePublishMessageStore = CapPrefix + nameof(WritePublishMessageStoreBefore);
        public const string CapAfterPublishMessageStore = CapPrefix + nameof(WritePublishMessageStoreAfter);
        public const string CapErrorPublishMessageStore = CapPrefix + nameof(WritePublishMessageStoreError);

        public const string CapBeforePublish = CapPrefix + nameof(WritePublishBefore);
        public const string CapAfterPublish = CapPrefix + nameof(WritePublishAfter);
        public const string CapErrorPublish = CapPrefix + nameof(WritePublishError);

        public const string CapBeforeConsume = CapPrefix + nameof(WriteConsumeBefore);
        public const string CapAfterConsume = CapPrefix + nameof(WriteConsumeAfter);
        public const string CapErrorConsume = CapPrefix + nameof(WriteConsumeError);

        public const string CapBeforeSubscriberInvoke = CapPrefix + nameof(WriteSubscriberInvokeBefore);
        public const string CapAfterSubscriberInvoke = CapPrefix + nameof(WriteSubscriberInvokeAfter);
        public const string CapErrorSubscriberInvoke = CapPrefix + nameof(WriteSubscriberInvokeError);


        //============================================================================
        //====================  Before publish store message      ====================
        //============================================================================
        public static void WritePublishMessageStoreBefore(this DiagnosticListener @this, BrokerStoreEventData eventData)
        {
            if (@this.IsEnabled(CapBeforePublishMessageStore))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapBeforePublishMessageStore, eventData);
            }
        }

        public static void WritePublishMessageStoreAfter(this DiagnosticListener @this,
            Guid operationId,
            CapPublishedMessage message,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterPublishMessageStore))
            {
                @this.Write(CapAfterPublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageId = message.Id,
                    MessageName = message.Name,
                    MessageContent = message.Content,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static void WritePublishMessageStoreError(this DiagnosticListener @this,
            Guid operationId,
            CapPublishedMessage message,
            Exception ex,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorPublishMessageStore))
            {
                @this.Write(CapErrorPublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageName = message.Name,
                    MessageContent = message.Content,
                    Exception = ex,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }


        //============================================================================
        //====================                  Publish           ====================
        //============================================================================
        public static void WritePublishBefore(this DiagnosticListener @this, BrokerPublishEventData eventData)
        {
            if (@this.IsEnabled(CapBeforePublish))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapBeforePublish, eventData);
            }
        }

        public static void WritePublishAfter(this DiagnosticListener @this, BrokerPublishEndEventData eventData)
        {
            if (@this.IsEnabled(CapAfterPublish))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapAfterPublish, eventData);
            }
        }

        public static void WritePublishError(this DiagnosticListener @this, BrokerPublishErrorEventData eventData)
        {
            if (@this.IsEnabled(CapErrorPublish))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapErrorPublish, eventData);
            }
        }


        //============================================================================
        //====================                  Consume           ====================
        //============================================================================
        public static Guid WriteConsumeBefore(this DiagnosticListener @this, BrokerConsumeEventData eventData)
        {
            if (@this.IsEnabled(CapBeforeConsume))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapBeforeConsume, eventData);
            }

            return Guid.Empty;
        }

        public static void WriteConsumeAfter(this DiagnosticListener @this, BrokerConsumeEndEventData eventData)
        {
            if (@this.IsEnabled(CapAfterConsume))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapAfterConsume, eventData);
            }
        }

        public static void WriteConsumeError(this DiagnosticListener @this, BrokerConsumeErrorEventData eventData)
        {
            if (@this.IsEnabled(CapErrorConsume))
            {
                eventData.Headers = new TracingHeaders();
                @this.Write(CapErrorConsume, eventData);
            }
        }


        //============================================================================
        //====================           SubscriberInvoke         ====================
        //============================================================================
        public static Guid WriteSubscriberInvokeBefore(this DiagnosticListener @this,
            ConsumerContext context,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforeSubscriberInvoke))
            {
                var operationId = Guid.NewGuid();

                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapBeforeSubscriberInvoke, new SubscriberInvokeEventData(operationId, operation, methodName,
                    subscribeName,
                    subscribeGroup, parameterValues, DateTimeOffset.UtcNow));

                return operationId;
            }

            return Guid.Empty;
        }

        public static void WriteSubscriberInvokeAfter(this DiagnosticListener @this,
            Guid operationId,
            ConsumerContext context,
            DateTimeOffset startTime,
            TimeSpan duration,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterSubscriberInvoke))
            {
                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapAfterSubscriberInvoke, new SubscriberInvokeEndEventData(operationId, operation, methodName,
                    subscribeName,
                    subscribeGroup, parameterValues, startTime, duration));
            }
        }

        public static void WriteSubscriberInvokeError(this DiagnosticListener @this,
            Guid operationId,
            ConsumerContext context,
            Exception ex,
            DateTimeOffset startTime,
            TimeSpan duration,
            int retries,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorSubscriberInvoke))
            {
                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapErrorSubscriberInvoke, new SubscriberInvokeErrorEventData(operationId, operation, methodName,
                    subscribeName,
                    subscribeGroup, parameterValues, ex, startTime, duration, retries));
            }
        }
    }
}