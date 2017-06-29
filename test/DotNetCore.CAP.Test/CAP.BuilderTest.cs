using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CapBuilderTest
    {
        [Fact]
        public void CanOverrideMessageStore()
        {
            var services = new ServiceCollection();
            services.AddConsistency().AddMessageStore<MyMessageStore>();

            var thingy = services.BuildServiceProvider()

                .GetRequiredService<ICapMessageStore>() as MyMessageStore;

            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideJobs()
        {
            var services = new ServiceCollection();
            services.AddConsistency().AddJobs<MyJobTest>();

            var thingy = services.BuildServiceProvider()
                .GetRequiredService<IJob>() as MyJobTest;

            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideProducerService()
        {
            var services = new ServiceCollection();
            services.AddConsistency().AddProducerService<MyProducerService>();

            var thingy = services.BuildServiceProvider()
                .GetRequiredService<ICapProducerService>() as MyProducerService;

            Assert.NotNull(thingy);
        }


        private class MyProducerService : ICapProducerService
        {
            public Task SendAsync(string topic, string content)
            {
                throw new NotImplementedException();
            }

            public Task SendAsync<T>(string topic, T contentObj) {
                throw new NotImplementedException();
            }
        }


        private class MyJobTest : IJob
        {
            public Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class MyMessageStore : ICapMessageStore
        {
            public Task<OperateResult> ChangeReceivedMessageStateAsync(CapReceivedMessage message, string statusName, bool autoSaveChanges = true)
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> ChangeSentMessageStateAsync(CapSentMessage message, string statusName, bool autoSaveChanges = true)
            {
                throw new NotImplementedException();
            }

            public Task<CapReceivedMessage> GetNextReceivedMessageToBeExcuted()
            {
                throw new NotImplementedException();
            }

            public Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync()
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> RemoveSentMessageAsync(CapSentMessage message)
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> StoreReceivedMessageAsync(CapReceivedMessage message)
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> StoreSentMessageAsync(CapSentMessage message)
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> UpdateReceivedMessageAsync(CapReceivedMessage message)
            {
                throw new NotImplementedException();
            }

            public Task<OperateResult> UpdateSentMessageAsync(CapSentMessage message)
            {
                throw new NotImplementedException();
            }
        }
    }
}