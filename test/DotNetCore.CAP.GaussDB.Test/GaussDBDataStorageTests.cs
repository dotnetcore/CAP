using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[Collection(GaussDBCollection.Name)]
public class GaussDBDataStorageTests
{
    [Fact]
    public async Task PublishedMessage_CoversStoreRetryDelayScheduleStateAndDelete()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var (storage, initializer) = await GaussDBTestSupport.CreateStorageAsync();
        var id = GaussDBTestSupport.NextId();
        var message = GaussDBTestSupport.CreateMessage(id);

        var stored = await storage.StoreMessageAsync("orders.created", message);
        var retryMessages = (await storage.GetPublishedMessagesOfNeedRetry(TimeSpan.FromSeconds(-1))).ToList();

        stored.ExpiresAt = DateTime.Now.AddSeconds(-1);
        await storage.ChangePublishStateAsync(stored, StatusName.Delayed);
        var scheduled = new List<MediumMessage>();
        await storage.ScheduleMessagesOfDelayedAsync((_, messages) =>
        {
            scheduled = messages.ToList();
            return Task.CompletedTask;
        });

        await storage.ChangePublishStateToDelayedAsync([id]);
        await storage.ChangePublishStateAsync(stored, StatusName.Succeeded);

        Assert.Contains(retryMessages, item => item.DbId == id);
        Assert.Contains(scheduled, item => item.DbId == id);
        Assert.Equal(nameof(StatusName.Succeeded),
            await GaussDBTestSupport.GetStatusAsync(initializer.GetPublishedTableName(), id));
        Assert.Equal(1, await storage.DeletePublishedMessageAsync(long.Parse(id)));
        Assert.Null(await GaussDBTestSupport.GetStatusAsync(initializer.GetPublishedTableName(), id));
    }

    [Fact]
    public async Task ReceivedMessage_CoversStoreExceptionRetryStateDeleteAndExpiryCleanup()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var (storage, initializer) = await GaussDBTestSupport.CreateStorageAsync();
        var id = GaussDBTestSupport.NextId();
        var message = await storage.StoreReceivedMessageAsync(
            "orders.received", "workers", GaussDBTestSupport.CreateMessage(id));

        var retryMessages = (await storage.GetReceivedMessagesOfNeedRetry(TimeSpan.FromSeconds(-1))).ToList();

        message.ExpiresAt = DateTime.Now.AddSeconds(-1);
        await storage.ChangeReceiveStateAsync(message, StatusName.Succeeded);
        var deletedExpired = await storage.DeleteExpiresAsync(initializer.GetReceivedTableName(), DateTime.Now);

        var exceptionName = $"orders.exception.{Guid.NewGuid():N}";
        await storage.StoreReceivedExceptionMessageAsync(exceptionName, "workers", "{}");
        var exceptionCount = await GaussDBTestSupport.CountByNameAsync(initializer.GetReceivedTableName(), exceptionName);
        await GaussDBTestSupport.DeleteByNameAsync(initializer.GetReceivedTableName(), exceptionName);

        Assert.Contains(retryMessages, item => item.DbId == message.DbId);
        Assert.True(deletedExpired >= 1);
        Assert.Null(await GaussDBTestSupport.GetStatusAsync(initializer.GetReceivedTableName(), message.DbId));
        Assert.Equal(1, exceptionCount);
    }

    [Fact]
    public async Task StorageLock_IsExclusiveUntilReleased()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var (storage, _) = await GaussDBTestSupport.CreateStorageAsync();
        const string key = "publish_retry_v1";

        Assert.True(await storage.AcquireLockAsync(key, TimeSpan.FromMinutes(1), "one"));
        Assert.False(await storage.AcquireLockAsync(key, TimeSpan.FromMinutes(1), "two"));
        await storage.RenewLockAsync(key, TimeSpan.FromSeconds(5), "one");
        await storage.ReleaseLockAsync(key, "one");
        Assert.True(await storage.AcquireLockAsync(key, TimeSpan.FromMinutes(1), "two"));
        await storage.ReleaseLockAsync(key, "two");
    }

    [Fact]
    public async Task GetMonitoringApi_ReturnsGaussDBMonitoringApi()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var (storage, _) = await GaussDBTestSupport.CreateStorageAsync();

        Assert.IsType<GaussDBMonitoringApi>(storage.GetMonitoringApi());
    }
}
