using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Monitoring;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.Options;
using HuaweiCloud.GaussDB;

namespace DotNetCore.CAP.GaussDB
{
    /// <summary>
    /// GaussDB 的 CAP 监控查询实现，提供统计、分页、详情和小时级趋势数据。
    /// </summary>
    public class GaussDBMonitoringApi : IMonitoringApi
    {
        private readonly GaussDBOptions _options;
        private readonly string _pubName;
        private readonly string _recName;
        private readonly ISerializer _serializer;
        private DbConnectionExtensions.GaussDBCompatibilityMode? _databaseCompatibilityMode;

        /// <summary>
        /// 初始化 GaussDB 监控查询实现。
        /// </summary>
        /// <param name="options">GaussDB 连接配置。</param>
        /// <param name="initializer">存储初始化器，提供表名和兼容模式。</param>
        /// <param name="serializer">消息反序列化器，用于还原消息内容。</param>
        public GaussDBMonitoringApi(IOptions<GaussDBOptions> options, IStorageInitializer initializer, ISerializer serializer)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _pubName = initializer.GetPublishedTableName();
            _recName = initializer.GetReceivedTableName();
            _serializer = serializer;

            if (initializer is GaussDBStorageInitializer gaussDBStorageInitializer)
            {
                _databaseCompatibilityMode = (DbConnectionExtensions.GaussDBCompatibilityMode)gaussDBStorageInitializer.DBCompatibilityMode;
            }
        }

        /// <inheritdoc />
        public async Task<MediumMessage?> GetPublishedMessageAsync(long id)
        {
            return await GetMessageAsync(_pubName, id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<MediumMessage?> GetReceivedMessageAsync(long id)
        {
            return await GetMessageAsync(_recName, id).ConfigureAwait(false);
        }

        /// <summary>
        /// 汇总发布/接收消息在成功、失败和延迟状态下的数量。
        /// </summary>
        public async Task<StatisticsDto> GetStatisticsAsync()
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
            #region M-Compatibility 模式
                        $@"
SELECT
(
    SELECT COUNT(`Id`) FROM {_pubName} WHERE StatusName = N'Succeeded'
) AS PublishedSucceeded,
(
    SELECT COUNT(`Id`) FROM {_recName} WHERE StatusName = N'Succeeded'
) AS ReceivedSucceeded,
(
    SELECT COUNT(`Id`) FROM {_pubName} WHERE StatusName = N'Failed'
) AS PublishedFailed,
(
    SELECT COUNT(`Id`) FROM {_recName} WHERE StatusName = N'Failed'
) AS ReceivedFailed,
(
    SELECT COUNT(`Id`) FROM {_pubName} WHERE StatusName = N'Delayed'
) AS PublishedDelayed;"

            #endregion
            :
            #region GaussDB 的模式
                   $@"
SELECT
(
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Succeeded'
) AS ""PublishedSucceeded"",
(
    SELECT COUNT(""Id"") FROM {_recName} WHERE ""StatusName"" = N'Succeeded'
) AS ""ReceivedSucceeded"",
(
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Failed'
) AS ""PublishedFailed"",
(
    SELECT COUNT(""Id"") FROM {_recName} WHERE ""StatusName"" = N'Failed'
) AS ""ReceivedFailed"",
(
    SELECT COUNT(""Id"") FROM {_pubName} WHERE ""StatusName"" = N'Delayed'
) AS ""PublishedDelayed"";"
            #endregion
            ;

            var connection = _options.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);

            var statistics = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                var statisticsDto = new StatisticsDto();

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    statisticsDto.PublishedSucceeded = reader.GetInt32(0);
                    statisticsDto.ReceivedSucceeded = reader.GetInt32(1);
                    statisticsDto.PublishedFailed = reader.GetInt32(2);
                    statisticsDto.ReceivedFailed = reader.GetInt32(3);
                    statisticsDto.PublishedDelayed = reader.GetInt32(4);
                }

                return statisticsDto;
            }).ConfigureAwait(false);

            return statistics;
        }

        /// <summary>
        /// 按消息类型、状态、名称、分组和内容分页查询监控列表。
        /// </summary>
        public async Task<PagedQueryResult<MessageDto>> GetMessagesAsync(MessageQueryDto queryDto)
        {
            var tableName = queryDto.MessageType == MessageType.Publish ? _pubName : _recName;
            var where = string.Empty;
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var connection = _options.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);

            if (!string.IsNullOrEmpty(queryDto.StatusName))
            {
                where += mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                    " AND Lower(`StatusName`) = Lower(@StatusName)" :
                    " AND Lower(\"StatusName\") = Lower(@StatusName)";
            }

            if (!string.IsNullOrEmpty(queryDto.Name))
            {
                where += mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                    " AND Lower(`Name`) = Lower(@Name)" :
                    " AND Lower(\"Name\") = Lower(@Name)";
            }

            if (!string.IsNullOrEmpty(queryDto.Group))
            {
                where += mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                    " AND Lower(`Group`) = Lower(@Group)" :
                    " AND Lower(\"Group\") = Lower(@Group)";
            }

            if (!string.IsNullOrEmpty(queryDto.Content))
            {
                where += mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                    " AND `Content` ILike @Content" :
                    " AND \"Content\" ILike @Content";
            }
            // 排序字段
            var orderBy = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ? "`Added`" : "\"Added\"";
            var sqlQuery = $"SELECT * FROM {tableName} WHERE 1=1 {where} ORDER BY {orderBy} DESC OFFSET @Offset LIMIT @Limit";

            var count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM {tableName} WHERE 1=1 {where}",
                new GaussDBParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new GaussDBParameter("@Group", queryDto.Group ?? string.Empty),
                new GaussDBParameter("@Name", queryDto.Name ?? string.Empty),
                new GaussDBParameter("@Content", $"%{queryDto.Content}%")).ConfigureAwait(false);

            object[] sqlParams =
            {
                new GaussDBParameter("@StatusName", queryDto.StatusName ?? string.Empty),
                new GaussDBParameter("@Group", queryDto.Group ?? string.Empty),
                new GaussDBParameter("@Name", queryDto.Name ?? string.Empty),
                new GaussDBParameter("@Content", $"%{queryDto.Content}%"),
                new GaussDBParameter("@Offset", queryDto.CurrentPage * queryDto.PageSize),
                new GaussDBParameter("@Limit", queryDto.PageSize)
            };

            var items = await connection.ExecuteReaderAsync(sqlQuery, async reader =>
            {
                var messages = new List<MessageDto>();

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var index = 0;
                    messages.Add(new MessageDto
                    {
                        Id = reader.GetInt64(index++).ToString(),
                        Version = reader.GetString(index++),
                        Name = reader.GetString(index++),
                        Group = queryDto.MessageType == MessageType.Subscribe ? reader.GetString(index++) : default,
                        Content = reader.GetString(index++),
                        Retries = reader.GetInt32(index++),
                        Added = reader.GetDateTime(index++),
                        ExpiresAt = reader.IsDBNull(index++) ? null : reader.GetDateTime(index - 1),
                        StatusName = reader.GetString(index)
                    });
                }

                return messages;
            }, sqlParams: sqlParams).ConfigureAwait(false);

            return new PagedQueryResult<MessageDto>
            { Items = items, PageIndex = queryDto.CurrentPage, PageSize = queryDto.PageSize, Totals = count };
        }

        /// <inheritdoc />
        public ValueTask<int> PublishedFailedCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Failed));
        }

        /// <inheritdoc />
        public ValueTask<int> PublishedSucceededCount()
        {
            return GetNumberOfMessage(_pubName, nameof(StatusName.Succeeded));
        }

        /// <inheritdoc />
        public ValueTask<int> ReceivedFailedCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Failed));
        }

        /// <inheritdoc />
        public ValueTask<int> ReceivedSucceededCount()
        {
            return GetNumberOfMessage(_recName, nameof(StatusName.Succeeded));
        }

        /// <inheritdoc />
        public async Task<IDictionary<DateTime, int>> HourlySucceededJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Succeeded)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IDictionary<DateTime, int>> HourlyFailedJobs(MessageType type)
        {
            var tableName = type == MessageType.Publish ? _pubName : _recName;
            return await GetHourlyTimelineStats(tableName, nameof(StatusName.Failed)).ConfigureAwait(false);
        }

        private async ValueTask<int> GetNumberOfMessage(string tableName, string statusName)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var idFile = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ? "`Id`" : "\"Id\"";
            var statusNameFile = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ? "`StatusName`" : "\"StatusName\"";

            var sqlQuery = $"SELECT COUNT({idFile}) FROM {tableName} WHERE Lower({statusNameFile}) = Lower(@State)";

            var connection = _options.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<int>(sqlQuery, new GaussDBParameter("@State", statusName))
                .ConfigureAwait(false);
        }

        private Task<Dictionary<DateTime, int>> GetHourlyTimelineStats(string tableName, string statusName)
        {
            var endDate = DateTime.Now;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => x.ToString("yyyy-MM-dd-HH"), x => x);

            return GetTimelineStats(tableName, statusName, keyMaps);
        }

        /// <summary>
        /// 查询数据库中已有的小时聚合数据，并补齐没有消息的小时为 0。
        /// </summary>
        private async Task<Dictionary<DateTime, int>> GetTimelineStats(
            string tableName,
            string statusName,
            IDictionary<string, DateTime> keyMaps)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sqlQuery = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
     $@"
SELECT Aggr.*
FROM (
         SELECT DATE_FORMAT(`Added`, '%Y-%m-%d-%H') AS `Key`,COUNT(`Id`) AS `Count`
         FROM {tableName}
         WHERE `StatusName` = @StatusName
         GROUP BY DATE_FORMAT(`Added`, '%Y-%m-%d-%H')
     ) AS Aggr
WHERE `Key` >= @MinKey AND `Key` <= @MaxKey;"
      :
            $@"
WITH Aggr AS (
    SELECT to_char(""Added"",'yyyy-MM-dd-HH') AS ""Key"",
    COUNT(""Id"") AS ""Count""
    FROM {tableName}
        WHERE ""StatusName"" = @StatusName
    GROUP BY to_char(""Added"", 'yyyy-MM-dd-HH')
)
SELECT ""Key"",""Count"" from Aggr WHERE ""Key"" >= @MinKey AND ""Key"" <= @MaxKey;";

            object[] sqlParams =
            {
                new GaussDBParameter("@StatusName", statusName),
                new GaussDBParameter("@MinKey", keyMaps.Keys.Min()),
                new GaussDBParameter("@MaxKey", keyMaps.Keys.Max())
            };

            Dictionary<string, int> valuesMap;
            var connection = _options.CreateConnection();
            await using (connection.ConfigureAwait(false))
            {
                valuesMap = await connection.ExecuteReaderAsync(sqlQuery, async reader =>
                {
                    var dictionary = new Dictionary<string, int>();

                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        dictionary.Add(reader.GetString(0), reader.GetInt32(1));
                    }

                    return dictionary;
                }, sqlParams: sqlParams).ConfigureAwait(false);
            }

            foreach (var key in keyMaps.Keys)
            {
                valuesMap.TryAdd(key, 0);
            }

            var result = new Dictionary<DateTime, int>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        /// <summary>
        /// 获取单条消息详情；使用 SKIP LOCKED 避免读取正在被其它工作线程处理的记录。
        /// </summary>
        private async Task<MediumMessage?> GetMessageAsync(string tableName, long id)
        {
            // 数据库的兼容模式
            var mode = await TryGetGaussDBCompatibilityModeAsync().ConfigureAwait(false);

            var sql = mode == DbConnectionExtensions.GaussDBCompatibilityMode.M_Compatibility ?
                 $"SELECT `Id` as DbId, `Content`,`Added`,`ExpiresAt`,`Retries` FROM {tableName} WHERE Id={id};" :
                 $@"SELECT ""Id"" AS ""DbId"", ""Content"", ""Added"", ""ExpiresAt"", ""Retries"" FROM {tableName} WHERE ""Id""={id} FOR UPDATE SKIP LOCKED";

            var connection = _options.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            var mediumMessage = await connection.ExecuteReaderAsync(sql, async reader =>
            {
                MediumMessage? message = null;

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    message = new MediumMessage
                    {
                        DbId = reader.GetInt64(0).ToString(),
                        Origin = _serializer.Deserialize(reader.GetString(1))!,
                        Content = reader.GetString(1),
                        Added = reader.GetDateTime(2),
                        ExpiresAt = reader.GetDateTime(3),
                        Retries = reader.GetInt32(4)
                    };
                }

                return message;
            }).ConfigureAwait(false);

            return mediumMessage;
        }

        /// <summary>
        /// 获取数据库的兼容模式
        /// </summary>
        /// <returns></returns>
        private async Task<DbConnectionExtensions.GaussDBCompatibilityMode> TryGetGaussDBCompatibilityModeAsync()
        {
            if (_databaseCompatibilityMode.HasValue) return _databaseCompatibilityMode.Value;
            var connection = _options.CreateConnection();
            try
            {
                _databaseCompatibilityMode = await connection.GetGaussDBCompatibilityModeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                connection?.Dispose();
            }
            return _databaseCompatibilityMode.Value;
        }
    }
}
