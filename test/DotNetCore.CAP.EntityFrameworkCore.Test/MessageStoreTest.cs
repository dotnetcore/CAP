using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class MessageStoreTest : MessageManagerTestBase, IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public MessageStoreTest(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        protected override bool ShouldSkipDbTests()
        {
            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
        }

        public class ApplicationDbContext : CapDbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
            {
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void CanCreateSentMessageUsingEF()
        {
            using (var db = CreateContext())
            {
                var guid = Guid.NewGuid().ToString();
                db.CapSentMessages.Add(new CapSentMessage
                {
                    Id = guid,
                    Content = "this is message body",
                    StateName = StateName.Enqueued
                });

                db.SaveChanges();
                Assert.True(db.CapSentMessages.Any(u => u.Id == guid));
                Assert.NotNull(db.CapSentMessages.FirstOrDefault(u => u.StateName == StateName.Enqueued));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var message = new CapSentMessage
            {
                Id = guid,
                Content = "this is message body",
                StateName = StateName.Enqueued,
            };

            var result = await manager.StoreSentMessageAsync(message);
            Assert.NotNull(result);
            Assert.True(result.Succeeded);

            result = await manager.RemoveSentMessageAsync(message);
            Assert.NotNull(result);
            Assert.True(result.Succeeded);
        }

        public CapDbContext CreateContext(bool delete = false)
        {
            var db = DbUtil.Create<CapDbContext>(_fixture.ConnectionString);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        protected override void AddMessageStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<ICapMessageStore>(new CapMessageStore<CapDbContext>((CapDbContext)context));
        }

        protected override CapSentMessage CreateTestSentMessage(string content = "")
        {
            return new CapSentMessage
            {
                Content = content
            };
        }

        protected override CapReceivedMessage CreateTestReceivedMessage(string content = "")
        {
            return new CapReceivedMessage()
            {
                Content = content
            };
        }
    }    
}