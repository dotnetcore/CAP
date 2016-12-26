using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cap.Consistency.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cap.Consistency.EntityFrameworkCore.Test
{
    public class MessageStoreTest : MessageManagerTestBase<ConsistencyMessage>, IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public MessageStoreTest(ScratchDatabaseFixture fixture) {
            _fixture = fixture;
        }

        protected override bool ShouldSkipDbTests() {
            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
        }

        public class ApplicationDbContext : ConsistencyDbContext<ApplicationMessage, string>
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void CanCreateMessageUsingEF() {
            using (var db = CreateContext()) {
                var guid = Guid.NewGuid().ToString();
                db.Messages.Add(new ConsistencyMessage {
                    Id = guid,
                    Payload = "this is message body",
                    Status = MessageStatus.WaitForSend,
                    SendTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();
                Assert.True(db.Messages.Any(u => u.Id == guid));
                Assert.NotNull(db.Messages.FirstOrDefault(u => u.Status == MessageStatus.WaitForSend));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanCreateUsingManager() {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var message = new ConsistencyMessage {
                Id = guid,
                Payload = "this is message body",
                Status = MessageStatus.WaitForSend,
                SendTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            var result = await manager.CreateAsync(message);
            Assert.NotNull(result);
            Assert.True(result.Succeeded);

            result = await manager.DeleteAsync(message);
            Assert.NotNull(result);
            Assert.True(result.Succeeded);
        }


        public ConsistencyDbContext CreateContext(bool delete = false) {
            var db = DbUtil.Create<ConsistencyDbContext>(_fixture.ConnectionString);
            if (delete) {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext() {
            return CreateContext();
        }

        protected override ConsistencyMessage CreateTestMessage(string payload = "") {
            return new ConsistencyMessage {
                Payload = payload
            };
        }

        protected override void AddMessageStore(IServiceCollection services, object context = null) {
            services.AddSingleton<IConsistencyMessageStore<ConsistencyMessage>>(new ConsistencyMessageStore<ConsistencyMessage>((ConsistencyDbContext)context));
        }
    }

    public class ApplicationMessage : ConsistencyMessage { }
}
