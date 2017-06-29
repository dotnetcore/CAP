using System;
using System.Linq;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class MessageStoreTest : DatabaseTestHost
    {
        [Fact]
        public void CanCreateSentMessageUsingEF()
        {
            using (var db = CreateContext())
            {
                var guid = Guid.NewGuid().ToString();
                var message = new CapSentMessage
                {
                    Id = guid,
                    Content = "this is message body",
                    StatusName = StatusName.Enqueued
                };
                db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

                db.SaveChanges();
                
                Assert.True(db.CapSentMessages.Any(u => u.Id == guid));
                Assert.NotNull(db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued));
            }
        }

        //[Fact]
        //public async Task CanCreateUsingManager()
        //{
        //    var manager = CreateManager();
        //    var guid = Guid.NewGuid().ToString();
        //    var message = new CapSentMessage
        //    {
        //        Id = guid,
        //        Content = "this is message body",
        //        StateName = StateName.Enqueued,
        //    };

        //    var result = await manager.StoreSentMessageAsync(message);
        //    Assert.NotNull(result);
        //    Assert.True(result.Succeeded);

        //    result = await manager.RemoveSentMessageAsync(message);
        //    Assert.NotNull(result);
        //    Assert.True(result.Succeeded);
        //}

        public TestDbContext CreateContext(bool delete = false)
        {
            var db = Provider.GetRequiredService<TestDbContext>();
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }
    }
}