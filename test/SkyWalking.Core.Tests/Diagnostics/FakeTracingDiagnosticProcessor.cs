using System;
using SkyWalking.Diagnostics;
using Xunit;
    
namespace SkyWalking.Core.Tests.Diagnostics
{
    public class FakeTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        public string ListenerName { get; } = FakeDiagnosticListener.ListenerName;
        
        public DateTime Timestamp { get; set; }

        [DiagnosticName(FakeDiagnosticListener.Executing)]
        public void Executing(
            [PropertyAttribute(Name = "Name")] string eventName, 
            [Property] DateTime Timestamp)
        {
            Assert.Equal("Executing", eventName);
            this.Timestamp = Timestamp;
        }

        [DiagnosticName(FakeDiagnosticListener.Executed)]
        public void Executed([Object] FakeDiagnosticListenerData data)
        {
            Assert.Equal("Executed", data.Name);
            Timestamp = data.Timestamp;
        }
    }
}