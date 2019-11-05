using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;

namespace DotNetCore.CAP.Persistence
{
    public interface IDataStorage
    {
        Task ChangePublishStateAsync(MediumMessage message, StatusName state);

        Task ChangeReceiveStateAsync(MediumMessage message, StatusName state);

        Task<MediumMessage> StoreMessageAsync(string name, Message content, object dbTransaction = null,
            CancellationToken cancellationToken = default);

        Task StoreReceivedExceptionMessageAsync(string name, string group, string content);

        Task<MediumMessage> StoreReceivedMessageAsync(string name, string group, Message content);

        Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default);

        Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry();

        Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry();

        //dashboard api
        IMonitoringApi GetMonitoringApi();
    }
}