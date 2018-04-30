using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SkyWalking.Config;
using Xunit;

namespace SkyWalking.Diagnostics.HttpClient.Tests
{
    public class HttpClientDiagnosticTests
    {
        [Fact]
        public async Task HttpClient_Request_Success_Test()
        {
            //Todo fix ci
            /*AgentConfig.ApplicationCode = "HttpClientDiagnosticTests";
            CollectorConfig.DirectServers = "HttpClientDiagnosticTests.xx:50000";

            var httpClientDiagnosticProcessor = new HttpClientDiagnosticProcessor();

            var observer = new TracingDiagnosticProcessorObserver(new ITracingDiagnosticProcessor[]
                {httpClientDiagnosticProcessor});

            DiagnosticListener.AllListeners.Subscribe(observer);

            using (var tracerContextListener = new FakeIgnoreTracerContextListener())
            {
                var httpClient = new System.Net.Http.HttpClient();
                await httpClient.GetAsync("https://github.com");
                Assert.Equal(1, tracerContextListener.Counter);
            }*/
        }

        [Fact]
        public async Task HttpClient_Request_Fail_Test()
        {
            AgentConfig.ApplicationCode = "HttpClientDiagnosticTests";
            CollectorConfig.DirectServers = "HttpClientDiagnosticTests.xx:50000";
            
            var httpClientDiagnosticProcessor = new HttpClientDiagnosticProcessor();
            
            var observer = new TracingDiagnosticProcessorObserver(new ITracingDiagnosticProcessor[]
                {httpClientDiagnosticProcessor});
            
            DiagnosticListener.AllListeners.Subscribe(observer);

            using (var tracerContextListener = new FakeIgnoreTracerContextListener())
            {
                try
                {
                    var httpClient = new System.Net.Http.HttpClient();
                    await httpClient.GetAsync("http://HttpClientDiagnosticTests.xx");
                }
                catch (Exception e)
                {
                }
              
                Assert.Equal(1, tracerContextListener.Counter);
            }
        }
    }
}
