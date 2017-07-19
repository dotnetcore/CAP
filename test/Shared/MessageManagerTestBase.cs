//using System;
//using System.Threading.Tasks;
//using DotNetCore.CAP.Infrastructure;
//using DotNetCore.CAP.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Xunit;

//namespace DotNetCore.CAP.Test
//{
//    public abstract class MessageManagerTestBase
//    {
//        private const string NullValue = "(null)";

//        protected virtual bool ShouldSkipDbTests()
//        {
//            return false;
//        }

//        protected virtual void SetupMessageServices(IServiceCollection services, object context = null)
//        {
//            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//            services.AddCap();
//            AddMessageStore(services, context);

//            services.AddSingleton<ILogger<ICapMessageStore>>(new TestLogger<ICapMessageStore>());
//        }

//        protected virtual ICapMessageStore CreateManager(object context = null, IServiceCollection services = null,
//            Action<IServiceCollection> configureServices = null)
//        {
//            if (services == null)
//            {
//                services = new ServiceCollection();
//            }
//            if (context == null)
//            {
//                context = CreateTestContext();
//            }
//            SetupMessageServices(services, context);

//            configureServices?.Invoke(services);

//            return services.BuildServiceProvider().GetService<ICapMessageStore>();
//        }

//        protected abstract object CreateTestContext();

//        protected abstract CapSentMessage CreateTestSentMessage(string content = "");
//        protected abstract CapReceivedMessage CreateTestReceivedMessage(string content = "");

//        protected abstract void AddMessageStore(IServiceCollection services, object context = null);

//        [Fact]
//        public async Task CanDeleteSentMessage()
//        {
//            if (ShouldSkipDbTests())
//            {
//                return;
//            }

//            var manager = CreateManager();
//            var message = CreateTestSentMessage();
//            var operateResult = await manager.StoreSentMessageAsync(message);
//            Assert.NotNull(operateResult);
//            Assert.True(operateResult.Succeeded);

//           // operateResult = await manager.RemoveSentMessageAsync(message);
//          //  Assert.NotNull(operateResult);
//           // Assert.True(operateResult.Succeeded);
//        }

//        //[Fact]
//        //public async Task CanUpdateReceivedMessage()
//        //{
//        //    if (ShouldSkipDbTests())
//        //    {
//        //        return;
//        //    }

//        //    var manager = CreateManager();
//        //    var message = CreateTestReceivedMessage();
//        //  //  var operateResult = await manager.StoreReceivedMessageAsync(message);
//        //  //  Assert.NotNull(operateResult);
//        //  //  Assert.True(operateResult.Succeeded);

//        //  //  message.StatusName = StatusName.Processing;
//        //  //  operateResult = await manager.UpdateReceivedMessageAsync(message);
//        //  //  Assert.NotNull(operateResult);
//        //  //  Assert.True(operateResult.Succeeded);
//        //}

//        [Fact]
//        public async Task CanGetNextSendMessage()
//        {
//            if (ShouldSkipDbTests())
//            {
//                return;
//            }
//            var manager = CreateManager();
//            var message = CreateTestSentMessage();

//            var operateResult = await manager.StoreSentMessageAsync(message);
//            Assert.NotNull(operateResult);
//            Assert.True(operateResult.Succeeded);

//           // var storeMessage = await manager.GetNextSentMessageToBeEnqueuedAsync();

//           // Assert.Equal(message, storeMessage);
//        }
//    }
//}