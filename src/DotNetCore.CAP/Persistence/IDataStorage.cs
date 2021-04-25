using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;

namespace DotNetCore.CAP.Persistence
{
    public partial interface IDataStorage
    {
        Task ChangePublishStateAsync(IMediumMessage message, StatusName state);

        Task ChangeReceiveStateAsync(IMediumMessage message, StatusName state);

        IMediumMessage StoreMessage(string name, ICapMessage content, object dbTransaction = null);


        void StoreReceivedExceptionMessage(string name, string group, string content);

        IMediumMessage StoreReceivedMessage(string name, string group, ICapMessage content);

        Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default);

        Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry();

        Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry();

        //dashboard api
        IMonitoringApi GetMonitoringApi();
    }

    public partial interface IDataStorage
    {
        IMediumMessage StoreMessage<T>(string name, ICapMessage content, object dbTransaction = null);
        Task ChangePublishStateAsync<T>(IMediumMessage message, StatusName state);

        Task ChangeReceiveStateAsync<T>(IMediumMessage message, StatusName state);

        void StoreReceivedExceptionMessage<T>(string name, string group, string content);

        IMediumMessage StoreReceivedMessage<T>(string name, string group, ICapMessage content);

        Task<int> DeleteExpiresAsync<T>(string table, DateTime timeout, int batchCount = 1000,
            CancellationToken token = default);

        Task<IEnumerable<IMediumMessage>> GetPublishedMessagesOfNeedRetry<T>();

        Task<IEnumerable<IMediumMessage>> GetReceivedMessagesOfNeedRetry<T>();
    }
}