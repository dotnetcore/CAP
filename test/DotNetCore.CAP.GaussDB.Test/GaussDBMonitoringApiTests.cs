using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Internal;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[Collection(GaussDBCollection.Name)]
public class GaussDBMonitoringApiTests
{
    [Fact]
    public async Task MonitoringApi_CoversStatisticsPagedDetailCountsAndHourlyTimeline()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        var (storage, initializer) = await GaussDBTestSupport.CreateStorageAsync();
        var api = storage.GetMonitoringApi();
        var publishedId = GaussDBTestSupport.NextId();
        var published = await storage.StoreMessageAsync(
            "gaussdb.monitoring.publish", GaussDBTestSupport.CreateMessage(publishedId));
        published.ExpiresAt = DateTime.Now.AddMinutes(10);
        await storage.ChangePublishStateAsync(published, StatusName.Succeeded);

        var received = await storage.StoreReceivedMessageAsync(
            "gaussdb.monitoring.receive", "monitoring-workers", GaussDBTestSupport.CreateMessage(GaussDBTestSupport.NextId()));
        received.ExpiresAt = DateTime.Now.AddMinutes(10);
        await storage.ChangeReceiveStateAsync(received, StatusName.Failed);

        var statistics = await api.GetStatisticsAsync();
        var publishedPage = await api.GetMessagesAsync(new MessageQueryDto
        {
            MessageType = MessageType.Publish,
            CurrentPage = 0,
            PageSize = 20,
            StatusName = nameof(StatusName.Succeeded),
            Name = "gaussdb.monitoring.publish"
        });
        var receivedPage = await api.GetMessagesAsync(new MessageQueryDto
        {
            MessageType = MessageType.Subscribe,
            CurrentPage = 0,
            PageSize = 20,
            Group = "monitoring-workers",
            Name = "gaussdb.monitoring.receive"
        });
        var publishedDetail = await api.GetPublishedMessageAsync(long.Parse(publishedId));
        var receivedDetail = await api.GetReceivedMessageAsync(long.Parse(received.DbId));
        var succeededTimeline = await api.HourlySucceededJobs(MessageType.Publish);
        var failedTimeline = await api.HourlyFailedJobs(MessageType.Subscribe);

        Assert.True(statistics.PublishedSucceeded >= 1);
        Assert.True(await api.PublishedSucceededCount() >= 1);
        Assert.True(await api.ReceivedFailedCount() >= 1);
        Assert.True(await api.PublishedFailedCount() >= 0);
        Assert.True(await api.ReceivedSucceededCount() >= 0);
        Assert.Contains(publishedPage.Items, item => item.Id == publishedId && item.Name == "gaussdb.monitoring.publish");
        Assert.Contains(receivedPage.Items, item => item.Id == received.DbId && item.Group == "monitoring-workers");
        Assert.NotNull(publishedDetail);
        Assert.NotNull(receivedDetail);
        Assert.Equal(24, succeededTimeline.Count);
        Assert.Equal(24, failedTimeline.Count);

        await storage.DeletePublishedMessageAsync(long.Parse(publishedId));
        await storage.DeleteReceivedMessageAsync(long.Parse(received.DbId));
    }
}
