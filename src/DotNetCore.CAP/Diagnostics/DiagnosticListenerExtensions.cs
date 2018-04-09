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

        public const string CapBeforeReceiveMessageStore = CapPrefix + nameof(WriteReceiveMessageStoreBefore);
        public const string CapAfterReceiveMessageStore = CapPrefix + nameof(WriteReceiveMessageStoreAfter);
        public const string CapErrorReceiveMessageStore = CapPrefix + nameof(WriteReceiveMessageStoreError);

        public const string CapBeforeConsumerInvoke = CapPrefix + nameof(WriteConsumerInvokeBefore);
        public const string CapAfterConsumerInvoke = CapPrefix + nameof(WriteConsumerInvokeAfter);
        public const string CapErrorConsumerInvoke = CapPrefix + nameof(WriteConsumerInvokeError);

        public static Guid WritePublishMessageStoreBefore(this DiagnosticListener @this, 
            CapPublishedMessage message,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforePublishMessageStore))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforePublishMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageName = message.Name,
                    MessageContent = message.Content
                });

                return operationId;
            }
            return Guid.Empty;
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

        public static Guid WritePublishBefore(this DiagnosticListener @this,
            string topic,
            string body,
            string brokerAddress, 
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforePublish))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforePublish, new BrokerPublishEventData(operationId, operation, brokerAddress, topic, body, DateTimeOffset.UtcNow));

                return operationId;
            }
            return Guid.Empty;
        }

        public static void WritePublishAfter(this DiagnosticListener @this, 
            Guid operationId, 
            string topic,
            string body,
            string brokerAddress,
            DateTimeOffset startTime, 
            TimeSpan duration,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterPublish))
            {
                @this.Write(CapAfterPublish, new BrokerPublishEndEventData(operationId, operation, brokerAddress, topic, body, startTime, duration));
            }
        }

        public static void WritePublishError(this DiagnosticListener @this, 
            Guid operationId,
            string topic, 
            string body,
            string brokerAddress,
            Exception ex,
            DateTimeOffset startTime, 
            TimeSpan duration, 
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorPublish))
            {
                @this.Write(CapErrorPublish, new BrokerPublishErrorEventData(operationId, operation, brokerAddress, topic, body, ex, startTime, duration));
            }
        }

        public static Guid WriteReceiveMessageStoreBefore(this DiagnosticListener @this,
            string topic, 
            string body,
            string groupName, 
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforeReceiveMessageStore))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforePublish, new BrokerConsumeEventData(operationId, operation, groupName, topic, body, DateTimeOffset.UtcNow));

                return operationId;
            }
            return Guid.Empty;
        }

        public static void WriteReceiveMessageStoreAfter(this DiagnosticListener @this,
            Guid operationId,
            string topic,
            string body,
            string groupName,
            DateTimeOffset startTime, 
            TimeSpan duration, 
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterReceiveMessageStore))
            {
                @this.Write(CapAfterPublish, new BrokerConsumeEndEventData(operationId, operation, groupName, topic, body, startTime, duration));
            }
        }

        public static void WriteReceiveMessageStoreError(this DiagnosticListener @this,
            Guid operationId, 
            string topic,
            string body,
            string groupName,
            Exception ex, 
            DateTimeOffset startTime,
            TimeSpan duration,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorReceiveMessageStore))
            {
                @this.Write(CapErrorPublish, new BrokerConsumeErrorEventData(operationId, operation, groupName, topic, body, ex, startTime, duration));
            }
        }

        public static Guid WriteConsumerInvokeBefore(this DiagnosticListener @this,
            ConsumerContext context,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforeConsumerInvoke))
            {
                Guid operationId = Guid.NewGuid();

                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapBeforePublish, new SubscriberInvokeEventData(operationId, operation, methodName, subscribeName,
                    subscribeGroup, parameterValues, DateTimeOffset.UtcNow));

                return operationId;
            }
            return Guid.Empty;
        }

        public static void WriteConsumerInvokeAfter(this DiagnosticListener @this,
            Guid operationId,
            ConsumerContext context,
            DateTimeOffset startTime,
            TimeSpan duration,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterConsumerInvoke))
            {
                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapBeforePublish, new SubscriberInvokeEndEventData(operationId, operation, methodName, subscribeName,
                    subscribeGroup, parameterValues, startTime, duration));
            }
        }

        public static void WriteConsumerInvokeError(this DiagnosticListener @this,
            Guid operationId,
            ConsumerContext context,
            Exception ex,
            DateTimeOffset startTime,
            TimeSpan duration,
            [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorConsumerInvoke))
            {
                var methodName = context.ConsumerDescriptor.MethodInfo.Name;
                var subscribeName = context.ConsumerDescriptor.Attribute.Name;
                var subscribeGroup = context.ConsumerDescriptor.Attribute.Group;
                var parameterValues = context.DeliverMessage.Content;

                @this.Write(CapBeforePublish, new SubscriberInvokeErrorEventData(operationId, operation, methodName, subscribeName,
                    subscribeGroup, parameterValues, ex, startTime, duration));
            }
        }
    }
}
