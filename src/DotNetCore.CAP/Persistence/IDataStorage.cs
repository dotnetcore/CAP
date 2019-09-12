using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Persistence
{
    public interface IDataStorage
    {
        Task ChangePublishStateAsync(MediumMessage message, StatusName state);

        Task ChangeReceiveStateAsync(MediumMessage message, StatusName state);

        Task<MediumMessage> StoreMessageAsync(string name, Message content, object dbTransaction = null, CancellationToken cancellationToken = default);

        Task<MediumMessage> StoreMessageAsync(string name, string group, Message content, CancellationToken cancellationToken = default);

        Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default);

        Task<IEnumerable<MediumMessage>> GetPublishedMessagesOfNeedRetry();

        Task<IEnumerable<MediumMessage>> GetReceivedMessagesOfNeedRetry();

        //Task<CapPublishedMessage> GetPublishedMessageAsync(long id);
        //Task<CapReceivedMessage> GetReceivedMessageAsync(long id);

        //public void UpdateMessage(CapPublishedMessage message)
        //{
        //    if (message == null)
        //    {
        //        throw new ArgumentNullException(nameof(message));
        //    }

        //    var sql =
        //        $"UPDATE `{_prefix}.published` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
        //    _dbConnection.Execute(sql, message);
        //}

        //public void UpdateMessage(CapReceivedMessage message)
        //{
        //    if (message == null)
        //    {
        //        throw new ArgumentNullException(nameof(message));
        //    }

        //    var sql = $"UPDATE `{_prefix}.received` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
        //    _dbConnection.Execute(sql, message);
        //}
    }
}
