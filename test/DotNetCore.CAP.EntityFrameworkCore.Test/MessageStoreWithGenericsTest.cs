//using System;
//using DotNetCore.CAP.Infrastructure;
//using DotNetCore.CAP.Store;
//using DotNetCore.CAP.Test;
//using Microsoft.AspNetCore.Testing;
//using Microsoft.Extensions.DependencyInjection;
//using Xunit;

//namespace DotNetCore.CAP.EntityFrameworkCore.Test
//{
//    public class MessageStoreWithGenericsTest : MessageManagerTestBase<MessageWithGenerics, string>, IClassFixture<ScratchDatabaseFixture>
//    {
//        private readonly ScratchDatabaseFixture _fixture;

//        public MessageStoreWithGenericsTest(ScratchDatabaseFixture fixture) {
//            _fixture = fixture;
//        }

//        protected override void AddMessageStore(IServiceCollection services, object context = null) {
//            services.AddSingleton<IConsistencyMessageStore>(new MessageStoreWithGenerics((ContextWithGenerics)context));
//        }

//        protected override object CreateTestContext() {
//            return CreateContext();
//        }

//        public ContextWithGenerics CreateContext() {
//            var db = DbUtil.Create<ContextWithGenerics>(_fixture.ConnectionString);
//            db.Database.EnsureCreated();
//            return db;
//        }

//        protected override MessageWithGenerics CreateTestMessage(string payload = "") {
//            return new MessageWithGenerics() {
//                Payload = payload,
//                SendTime = DateTime.Now,
//                Status = MessageStatus.WaitForSend,
//                UpdateTime = DateTime.Now
//            };
//        }

//        protected override bool ShouldSkipDbTests() {
//            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
//        }
//    }

//    public class MessageWithGenerics : ConsistencyMessage
//    {
//    }

//    public class MessageStoreWithGenerics : ConsistencyMessageStore<ContextWithGenerics>
//    {
//        public MessageStoreWithGenerics(ContextWithGenerics context) : base(context) {
//        }
//    }

//    public class ContextWithGenerics : ConsistencyDbContext
//    {
//        public ContextWithGenerics() {
//        }
//    }
//}