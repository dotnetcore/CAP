//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using DotNetCore.CAP.Infrastructure;
//using DotNetCore.CAP.Store;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Xunit;

//namespace DotNetCore.CAP.Test
//{
//    public abstract class MessageManagerTestBase<TMessage> : MessageManagerTestBase<TMessage, string>
//        where TMessage : ConsistencyMessage
//    {
//    }

//    public abstract class MessageManagerTestBase<TMessage, TKey>
//        where TMessage : ConsistencyMessage
//        where TKey : IEquatable<TKey>
//    {
//        private const string NullValue = "(null)";

//        protected virtual bool ShouldSkipDbTests() {
//            return false;
//        }

//        protected virtual void SetupMessageServices(IServiceCollection services, object context = null) {
//            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//            services.AddConsistency();
//            AddMessageStore(services, context);

//            services.AddSingleton<ILogger<IConsistencyMessageStore >>(new TestLogger<IConsistencyMessageStore >());
//        }

//        protected virtual IConsistencyMessageStore  CreateManager(object context = null, IServiceCollection services = null, Action<IServiceCollection> configureServices = null) {
//            if (services == null) {
//                services = new ServiceCollection();
//            }
//            if (context == null) {
//                context = CreateTestContext();
//            }
//            SetupMessageServices(services, context);

//            configureServices?.Invoke(services);

//            return services.BuildServiceProvider().GetService<IConsistencyMessageStore >();
//        }

//        protected abstract object CreateTestContext();

//        protected abstract TMessage CreateTestMessage(string payload = "");

//        protected abstract void AddMessageStore(IServiceCollection services, object context = null);

//        [Fact]
//        public async Task CanDeleteMessage() {
//            if (ShouldSkipDbTests()) {
//                return;
//            }

//            var manager = CreateManager();
//            var message = CreateTestMessage();
//            var operateResult = await manager.CreateAsync(message);
//            Assert.NotNull(operateResult);
//            Assert.True(operateResult.Succeeded);

//            var messageId = await manager.GeConsistencyMessageIdAsync(message);
//            operateResult = await manager.DeleteAsync(message);
//            Assert.Null(await manager.FindByIdAsync(messageId));
//        }

//        [Fact]
//        public async Task CanFindById() {
//            if (ShouldSkipDbTests()) {
//                return;
//            }
//            var manager = CreateManager();
//            var message = CreateTestMessage();

//            var operateResult = await manager.CreateAsync(message);
//            Assert.NotNull(operateResult);
//            Assert.True(operateResult.Succeeded);

//            var messageId = await manager.GeConsistencyMessageIdAsync(message);
//            Assert.NotNull(await manager.FindByIdAsync(messageId));
//        }
//    }

//}