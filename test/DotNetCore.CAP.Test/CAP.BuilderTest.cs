using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Data;

namespace DotNetCore.CAP.Test
{
    public class CapBuilderTest
    {
        //[Fact]
        //public void CanOverrideMessageStore()
        //{
        //    var services = new ServiceCollection();
        //    services.AddCap().AddMessageStore<MyMessageStore>();

        //    var thingy = services.BuildServiceProvider()
        //        .GetRequiredService<ICapMessageStore>() as MyMessageStore;

        //    Assert.NotNull(thingy);
        ////}

        //[Fact]
        //public void CanOverrideJobs()
        //{
        //    var services = new ServiceCollection();
        //    services.AddCap().AddJobs<MyJobTest>();

        //    var thingy = services.BuildServiceProvider()
        //        .GetRequiredService<IJob>() as MyJobTest;

        //    Assert.NotNull(thingy);
        //}

        //[Fact]
        //public void CanOverrideProducerService()
        //{
        //    var services = new ServiceCollection();
        //    services.AddCap(x=> { });

        //    var thingy = services.BuildServiceProvider()
        //        .GetRequiredService<ICapPublisher>() as MyProducerService;

        //    Assert.NotNull(thingy);
        //}


        private class MyProducerService : ICapPublisher
        {
            public Task PublishAsync(string topic, string content)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(string topic, T contentObj)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync(string topic, string content, IDbConnection dbConnection)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync(string topic, string content, IDbConnection dbConnection, IDbTransaction dbTransaction)
            {
                throw new NotImplementedException();
            }
        }  
    }
}