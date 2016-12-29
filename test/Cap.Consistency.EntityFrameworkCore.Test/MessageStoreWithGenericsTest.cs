using System;
using Cap.Consistency.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cap.Consistency.EntityFrameworkCore.Test
{
    public class MessageStoreWithGenericsTest : MessageManagerTestBase<MessageWithGenerics, string>, IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public MessageStoreWithGenericsTest(ScratchDatabaseFixture fixture) {
            _fixture = fixture;
        }

        protected override void AddMessageStore(IServiceCollection services, object context = null) {
            services.AddSingleton<IConsistencyMessageStore<MessageWithGenerics>>(new MessageStoreWithGenerics((ContextWithGenerics)context));
        }

        protected override object CreateTestContext() {
            return CreateContext();
        }

        public ContextWithGenerics CreateContext() {
            var db = DbUtil.Create<ContextWithGenerics>(_fixture.ConnectionString);
            db.Database.EnsureCreated();
            return db;
        }

        protected override MessageWithGenerics CreateTestMessage(string payload = "") {
            return new MessageWithGenerics() {
                Payload = payload,
                SendTime = DateTime.Now,
                Status = MessageStatus.WaitForSend,
                UpdateTime = DateTime.Now
            };
        }

        protected override bool ShouldSkipDbTests() {
            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
        }
    }

    public class MessageWithGenerics : ConsistencyMessage
    {
    }

    public class MessageStoreWithGenerics : ConsistencyMessageStore<MessageWithGenerics>
    {
        public MessageStoreWithGenerics(DbContext context) : base(context) {
        }
    }

    public class ContextWithGenerics : ConsistencyDbContext<MessageWithGenerics, string>
    {
        public ContextWithGenerics(DbContextOptions options) : base(options) {
        }
    }
}