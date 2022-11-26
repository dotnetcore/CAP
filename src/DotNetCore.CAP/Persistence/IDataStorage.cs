using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;

namespace DotNetCore.CAP.Persistence;

public interface IDataStorage
{
    Task ChangePublishStateToDelayedAsync(string[] ids);

    Task ChangePublishStateAsync(MediumMessage message, StatusName state, object? transaction = null);

    Task ChangeReceiveStateAsync(MediumMessage message, StatusName state);

    Task<MediumMessage> StoreMessageAsync(string name, Message content, object? transaction = null);

    Task StoreReceivedExceptionMessageAsync(string name, string group, string content);

    Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message content);

    Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default);

    Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry();

    Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MediumMessage>, Task> scheduleTask, CancellationToken token = default);

    Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry();

    //dashboard api
    IMonitoringApi GetMonitoringApi();
}