using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Internal;
using Xunit;

namespace DotNetCore.CAP.Test
{

    public class DiagnosticsTest
    {
        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(CapDiagnosticListenerExtensions.DiagnosticListenerName);

        [Fact]
        public void WritePublishBeforeTest()
        {
            Guid operationId = Guid.NewGuid();

            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerPublishEventData(operationId, "", "", "", "", DateTimeOffset.UtcNow);
                s_diagnosticListener.WritePublishBefore(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapBeforePublish))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerPublishEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerPublishEventData)kvp.Value).OperationId);
                }
            });
        }

        [Fact]
        public void WritePublishAfterTest()
        {
            Guid operationId = Guid.NewGuid();

            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerPublishEndEventData(operationId, "", "", "", "", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
                s_diagnosticListener.WritePublishAfter(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapAfterPublish))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerPublishEndEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerPublishEndEventData)kvp.Value).OperationId);
                    Assert.Equal(TimeSpan.FromMinutes(1), ((BrokerPublishEndEventData)kvp.Value).Duration);
                }
            });
        }

        [Fact]
        public void WritePublishErrorTest()
        {
            Guid operationId = Guid.NewGuid();
            var ex = new Exception("WritePublishErrorTest");
            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerPublishErrorEventData(operationId, "", "", "", "", ex, DateTimeOffset.UtcNow, default(TimeSpan), default(int));
                s_diagnosticListener.WritePublishError(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapErrorPublish))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerPublishErrorEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerPublishErrorEventData)kvp.Value).OperationId);
                    Assert.Equal(ex, ((BrokerPublishErrorEventData)kvp.Value).Exception);
                }
            });
        }

        [Fact]
        public void WriteConsumeBeforeTest()
        {
            Guid operationId = Guid.NewGuid();

            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerConsumeEventData(operationId, "", "", "", "", DateTimeOffset.UtcNow);
                s_diagnosticListener.WriteConsumeBefore(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapBeforeConsume))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerConsumeEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerConsumeEventData)kvp.Value).OperationId);
                }
            });
        }

        [Fact]
        public void WriteConsumeAfterTest()
        {
            Guid operationId = Guid.NewGuid();

            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerConsumeEndEventData(operationId, "", "", "", "", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
                s_diagnosticListener.WriteConsumeAfter(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapAfterConsume))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerConsumeEndEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerConsumeEndEventData)kvp.Value).OperationId);
                    Assert.Equal(TimeSpan.FromMinutes(1), ((BrokerConsumeEndEventData)kvp.Value).Duration);
                }
            });
        }

        [Fact]
        public void WriteConsumeErrorTest()
        {
            Guid operationId = Guid.NewGuid();
            var ex = new Exception("WriteConsumeErrorTest");
            DiagnosticsWapper(() =>
            {
                var eventData = new BrokerConsumeErrorEventData(operationId, "", "", "", "", ex, DateTimeOffset.UtcNow, default(TimeSpan));
                s_diagnosticListener.WriteConsumeError(eventData);

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapErrorPublish))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<BrokerConsumeErrorEventData>(kvp.Value);
                    Assert.Equal(operationId, ((BrokerConsumeErrorEventData)kvp.Value).OperationId);
                    Assert.Equal(ex, ((BrokerConsumeErrorEventData)kvp.Value).Exception);
                }
            });
        }

        [Fact]
        public void WriteSubscriberInvokeBeforeTest()
        {
            DiagnosticsWapper(() =>
            {
                s_diagnosticListener.WriteSubscriberInvokeBefore(FackConsumerContext());

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapBeforeSubscriberInvoke))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<SubscriberInvokeEventData>(kvp.Value);
                }
            });
        }

        [Fact]
        public void WriteSubscriberInvokeAfterTest()
        {
            Guid operationId = Guid.NewGuid();

            DiagnosticsWapper(() =>
            {
                s_diagnosticListener.WriteSubscriberInvokeAfter(operationId, FackConsumerContext(), DateTimeOffset.Now, TimeSpan.FromMinutes(1));

            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapAfterSubscriberInvoke))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<SubscriberInvokeEndEventData>(kvp.Value);
                    Assert.Equal(operationId, ((SubscriberInvokeEndEventData)kvp.Value).OperationId);

                }
            });
        }

        [Fact]
        public void WriteSubscriberInvokeErrorTest()
        {
            Guid operationId = Guid.NewGuid();

            var ex = new Exception("WriteConsumeErrorTest");
            DiagnosticsWapper(() =>
            {
                s_diagnosticListener.WriteSubscriberInvokeError(operationId, FackConsumerContext(), ex,
                    DateTimeOffset.Now, TimeSpan.MaxValue, default(int));
            }, kvp =>
            {
                if (kvp.Key.Equals(CapDiagnosticListenerExtensions.CapErrorSubscriberInvoke))
                {
                    Assert.NotNull(kvp.Value);
                    Assert.IsType<SubscriberInvokeErrorEventData>(kvp.Value);
                    Assert.Equal(operationId, ((SubscriberInvokeErrorEventData)kvp.Value).OperationId);
                    Assert.Equal(ex, ((SubscriberInvokeErrorEventData)kvp.Value).Exception);
                }
            });
        }

        private ConsumerContext FackConsumerContext()
        {
            //Mock description
            var description = new ConsumerExecutorDescriptor
            {
                MethodInfo = GetType().GetMethod("WriteSubscriberInvokeAfterTest"),
                Attribute = new CapSubscribeAttribute("xxx"),
                ImplTypeInfo = GetType().GetTypeInfo()
            };

            //Mock messageContext
            var messageContext = new MessageContext
            {
                Name= "Name",
                Group= "Group",
                Content = "Content"
            };
        
            return new ConsumerContext(description, messageContext);
        }

        private void DiagnosticsWapper(Action operation, Action<KeyValuePair<string, object>> assert, [CallerMemberName]string methodName = "")
        {
            FakeDiagnosticListenerObserver diagnosticListenerObserver = new FakeDiagnosticListenerObserver(assert);

            diagnosticListenerObserver.Enable();
            using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
            {
                Console.WriteLine(string.Format("Test: {0} Enabled Listeners", methodName));
                operation();
            }
        }
    }
}
