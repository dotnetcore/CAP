using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Extension methods on the DiagnosticListener class to log CAP data
    /// </summary>
    internal static class CapDiagnosticListenerExtensions
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

        public static Guid WritePublishMessageStoreBefore(this DiagnosticListener @this, CapPublishedMessage message, [CallerMemberName] string operation = "")
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

        public static void WritePublishMessageStoreAfter(this DiagnosticListener @this, Guid operationId, CapPublishedMessage message, [CallerMemberName] string operation = "")
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

        public static void WritePublishMessageStoreError(this DiagnosticListener @this, Guid operationId,
            CapPublishedMessage message, Exception ex, [CallerMemberName] string operation = "")
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

        public static Guid WritePublishBefore(this DiagnosticListener @this, CapPublishedMessage message, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforePublish))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforePublish, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageId = message.Id,
                    MessageName = message.Name,
                    MessageContent = message.Content
                });

                return operationId;
            }
            return Guid.Empty;
        }

        public static void WritePublishAfter(this DiagnosticListener @this, Guid operationId, CapPublishedMessage message, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterPublish))
            {
                @this.Write(CapAfterPublish, new
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

        public static void WritePublishError(this DiagnosticListener @this, Guid operationId,
            CapPublishedMessage message, Exception ex, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorPublish))
            {
                @this.Write(CapErrorPublish, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageId = message.Id,
                    MessageName = message.Name,
                    MessageContent = message.Content,
                    Exception = ex,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static Guid WriteReceiveMessageStoreBefore(this DiagnosticListener @this, CapReceivedMessage message, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforeReceiveMessageStore))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforeReceiveMessageStore, new
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

        public static void WriteReceiveMessageStoreAfter(this DiagnosticListener @this, Guid operationId, CapReceivedMessage message, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterReceiveMessageStore))
            {
                @this.Write(CapAfterReceiveMessageStore, new
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

        public static void WriteReceiveMessageStoreError(this DiagnosticListener @this, Guid operationId,
            CapReceivedMessage message, Exception ex, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorReceiveMessageStore))
            {
                @this.Write(CapErrorReceiveMessageStore, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MessageId = message.Id,
                    MessageName = message.Name,
                    MessageContent = message.Content,
                    Exception = ex,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static Guid WriteConsumerInvokeBefore(this DiagnosticListener @this, ConsumerContext context, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapBeforeConsumerInvoke))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(CapBeforeConsumerInvoke, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MethodName = context.ConsumerDescriptor.MethodInfo.Name,
                    ConsumerGroup = context.ConsumerDescriptor.Attribute.Group,
                    MessageName = context.DeliverMessage.Name,
                    MessageContent = context.DeliverMessage.Content,
                    Timestamp = Stopwatch.GetTimestamp()
                });

                return operationId;
            }
            return Guid.Empty;
        }

        public static void WriteConsumerInvokeAfter(this DiagnosticListener @this, Guid operationId, ConsumerContext context, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapAfterConsumerInvoke))
            {
                @this.Write(CapAfterConsumerInvoke, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MethodName = context.ConsumerDescriptor.MethodInfo.Name,
                    ConsumerGroup = context.ConsumerDescriptor.Attribute.Group,
                    MessageName = context.DeliverMessage.Name,
                    MessageContent = context.DeliverMessage.Content,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }

        public static void WriteConsumerInvokeError(this DiagnosticListener @this, Guid operationId,
            ConsumerContext context, Exception ex, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(CapErrorConsumerInvoke))
            {
                @this.Write(CapErrorConsumerInvoke, new
                {
                    OperationId = operationId,
                    Operation = operation,
                    MethodName = context.ConsumerDescriptor.MethodInfo.Name,
                    ConsumerGroup = context.ConsumerDescriptor.Attribute.Group,
                    MessageName = context.DeliverMessage.Name,
                    MessageContent = context.DeliverMessage.Content,
                    Exception = ex,
                    Timestamp = Stopwatch.GetTimestamp()
                });
            }
        }
    }
}
