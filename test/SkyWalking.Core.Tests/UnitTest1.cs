using System;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Config;
using SkyWalking.Context;
using SkyWalking.Context.Trace;
using SkyWalking.Remote;
using Xunit;

namespace SkyWalking.Core.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            AgentConfig.ApplicationCode = "skywalking.test";
            CollectorConfig.DirectServers = "localhost:11800";
            await GrpcChannelManager.Instance.ConnectAsync();
            await ServiceManager.Instance.Initialize();
            var appId = RemoteDownstreamConfig.Agent.ApplicationId;
            var appInsId = RemoteDownstreamConfig.Agent.ApplicationInstanceId;

            var span = ContextManager.CreateEntrySpan("test", new ContextCarrier());

            span.SetComponent("Skywalking.Core.Tests");
            
            span.AsHttp();
            
            ContextManager.StopSpan(span);
            
            
        }
    }
}
