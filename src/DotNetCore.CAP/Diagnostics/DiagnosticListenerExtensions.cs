// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Diagnostics
{
    /// <summary>
    /// Extension methods on the DiagnosticListener class to log CAP data
    /// </summary>
    public static class CapDiagnosticListenerExtensions
    {
        public const string DiagnosticListenerName = "CapDiagnosticListener";

        private const string CapPrefix = "DotNetCore.CAP.";

        public const string CapBeforePublishMessageStore = CapPrefix + "WritePublishMessageStoreBefore";
        public const string CapAfterPublishMessageStore = CapPrefix + "WritePublishMessageStoreAfter";
        public const string CapErrorPublishMessageStore = CapPrefix + "WritePublishMessageStoreError";

        public const string CapBeforePublish = CapPrefix + "WritePublishBefore";
        public const string CapAfterPublish = CapPrefix + "WritePublishAfter";
        public const string CapErrorPublish = CapPrefix + "WritePublishError";

        public const string CapBeforeConsume = CapPrefix + "WriteConsumeBefore";
        public const string CapAfterConsume = CapPrefix + "WriteConsumeAfter";
        public const string CapErrorConsume = CapPrefix + "WriteConsumeError";

        public const string CapBeforeSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeBefore";
        public const string CapAfterSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeAfter";
        public const string CapErrorSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeError";


        //============================================================================
        //====================  Before publish store message      ====================
        //============================================================================
        public static Guid WritePublishMessageStoreBefore(this DiagnosticListener @this,
            Message message,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforePublishMessageStore))
            {
                var operationId = Guid.NewGuid();

                @this.Write(CapBeforePublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    Message = message
                });

                return operationId;
            }

            return Guid.Empty;
        }

        public static void WritePublishMessageStoreAfter(this DiagnosticListener @this,
            Guid operationId,
            Message message,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterPublishMessageStore))
            {
                @this.Write(CapAfterPublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    Message = message,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static void WritePublishMessageStoreError(this DiagnosticListener @this,
            Guid operationId,
            Message message,
            Exception ex,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorPublishMessageStore))
            {
                @this.Write(CapErrorPublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    Message = message,
                    Exception = ex,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }


        ////============================================================================
        ////====================                  Publish           ====================
        ////============================================================================
        //public static void WritePublishBefore(this DiagnosticListener @this, BrokerPublishEventData eventData)
        //{
        //    if (@this.IsEnabled(CapBeforePublish))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapBeforePublish, eventData);
        //    }
        //}

        //public static void WritePublishAfter(this DiagnosticListener @this, BrokerPublishEndEventData eventData)
        //{
        //    if (@this.IsEnabled(CapAfterPublish))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapAfterPublish, eventData);
        //    }
        //}

        //public static void WritePublishError(this DiagnosticListener @this, BrokerPublishErrorEventData eventData)
        //{
        //    if (@this.IsEnabled(CapErrorPublish))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapErrorPublish, eventData);
        //    }
        //}


        //============================================================================
        //====================                  Consume           ====================
        //============================================================================
        //public static Guid WriteConsumeBefore(this DiagnosticListener @this, BrokerConsumeEventData eventData)
        //{
        //    if (@this.IsEnabled(CapBeforeConsume))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapBeforeConsume, eventData);
        //    }

        //    return Guid.Empty;
        //}

        //public static void WriteConsumeAfter(this DiagnosticListener @this, BrokerConsumeEndEventData eventData)
        //{
        //    if (@this.IsEnabled(CapAfterConsume))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapAfterConsume, eventData);
        //    }
        //}

        //public static void WriteConsumeError(this DiagnosticListener @this, BrokerConsumeErrorEventData eventData)
        //{
        //    if (@this.IsEnabled(CapErrorConsume))
        //    {
        //        eventData.Headers = new TracingHeaders();
        //        @this.Write(CapErrorConsume, eventData);
        //    }
        //}


        //============================================================================
        //====================           SubscriberInvoke         ====================
        //============================================================================
        //public static Guid WriteSubscriberInvokeBefore(this DiagnosticListener @this,
        //    ConsumerContext context,
        //    [CallerMemberName] string operation = "")
        //{
        //    if (@this.IsEnabled(CapBeforeSubscriberInvoke))
        //    {
        //        var operationId = Guid.NewGuid();

        //        var methodName = context.ConsumerDescriptor.MethodInfo.Name;
        //        var subscribeName = context.ConsumerDescriptor.Attribute.Name;
        //        var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
        //        var values = context.DeliverMessage.Value;

        //        @this.Write(CapBeforeSubscriberInvoke, new SubscriberInvokeEventData(operationId, operation, methodName,
        //            subscribeName,
        //            subscribeGroup, parameterValues, DateTimeOffset.UtcNow));

        //        return operationId;
        //    }

        //    return Guid.Empty;
        //}

        //public static void WriteSubscriberInvokeAfter(this DiagnosticListener @this,
        //    Guid operationId,
        //    ConsumerContext context,
        //    DateTimeOffset startTime,
        //    TimeSpan duration,
        //    [CallerMemberName] string operation = "")
        //{
        //    if (@this.IsEnabled(CapAfterSubscriberInvoke))
        //    {
        //        var methodName = context.ConsumerDescriptor.MethodInfo.Name;
        //        var subscribeName = context.ConsumerDescriptor.Attribute.Name;
        //        var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
        //        var values = context.DeliverMessage.Value;

        //        @this.Write(CapAfterSubscriberInvoke, new SubscriberInvokeEndEventData(operationId, operation, methodName,
        //            subscribeName,
        //            subscribeGroup, parameterValues, startTime, duration));
        //    }
        //}

        //public static void WriteSubscriberInvokeError(this DiagnosticListener @this,
        //    Guid operationId,
        //    ConsumerContext context,
        //    Exception ex,
        //    DateTimeOffset startTime,
        //    TimeSpan duration,
        //    [CallerMemberName] string operation = "")
        //{
        //    if (@this.IsEnabled(CapErrorSubscriberInvoke))
        //    {
        //        var methodName = context.ConsumerDescriptor.MethodInfo.Name;
        //        var subscribeName = context.ConsumerDescriptor.Attribute.Name;
        //        var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
        //        var parameterValues = context.DeliverMessage.Content;

        //        @this.Write(CapErrorSubscriberInvoke, new SubscriberInvokeErrorEventData(operationId, operation, methodName,
        //            subscribeName,
        //            subscribeGroup, parameterValues, ex, startTime, duration));
        //    }
        //}
    }
}