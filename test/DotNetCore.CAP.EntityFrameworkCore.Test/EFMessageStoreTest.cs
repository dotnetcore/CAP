namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    //public class EFMessageStoreTest : DatabaseTestHost
    //{
    //    [Fact]
    //    public void CanCreateSentMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapPublishedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            Assert.True(db.CapSentMessages.Any(u => u.Id == guid));
    //            Assert.NotNull(db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued));
    //        }
    //    }

    //    [Fact]
    //    public void CanUpdateSentMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapPublishedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            var selectedMessage = db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.NotNull(selectedMessage);

    //            selectedMessage.StatusName = StatusName.Succeeded;
    //            selectedMessage.Content = "Test";
    //            db.SaveChanges();

    //            selectedMessage = db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Succeeded);
    //            Assert.NotNull(selectedMessage);
    //            Assert.True(selectedMessage.Content == "Test");
    //        }
    //    }

    //    [Fact]
    //    public void CanRemoveSentMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapPublishedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            var selectedMessage = db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.NotNull(selectedMessage);

    //            db.CapSentMessages.Remove(selectedMessage);
    //            db.SaveChanges();
    //            selectedMessage = db.CapSentMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.Null(selectedMessage);
    //        }
    //    }

    //    [Fact]
    //    public void CanCreateReceivedMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapReceivedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            Assert.True(db.CapReceivedMessages.Any(u => u.Id == guid));
    //            Assert.NotNull(db.CapReceivedMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued));
    //        }
    //    }

    //    [Fact]
    //    public void CanUpdateReceivedMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapReceivedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            var selectedMessage = db.CapReceivedMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.NotNull(selectedMessage);

    //            selectedMessage.StatusName = StatusName.Succeeded;
    //            selectedMessage.Content = "Test";
    //            db.SaveChanges();

    //            selectedMessage = db.CapReceivedMessages.FirstOrDefault(u => u.StatusName == StatusName.Succeeded);
    //            Assert.NotNull(selectedMessage);
    //            Assert.True(selectedMessage.Content == "Test");
    //        }
    //    }

    //    [Fact]
    //    public void CanRemoveReceivedMessageUsingEF()
    //    {
    //        using (var db = CreateContext())
    //        {
    //            var guid = Guid.NewGuid().ToString();
    //            var message = new CapReceivedMessage
    //            {
    //                Id = guid,
    //                Content = "this is message body",
    //                StatusName = StatusName.Enqueued
    //            };
    //            db.Attach(message).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    //            db.SaveChanges();

    //            var selectedMessage = db.CapReceivedMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.NotNull(selectedMessage);

    //            db.CapReceivedMessages.Remove(selectedMessage);
    //            db.SaveChanges();
    //            selectedMessage = db.CapReceivedMessages.FirstOrDefault(u => u.StatusName == StatusName.Enqueued);
    //            Assert.Null(selectedMessage);
    //        }
    //    }

    //    public TestDbContext CreateContext(bool delete = false)
    //    {
    //        var db = Provider.GetRequiredService<TestDbContext>();
    //        if (delete)
    //        {
    //            db.Database.EnsureDeleted();
    //        }
    //        db.Database.EnsureCreated();
    //        return db;
    //    }
    //}
}