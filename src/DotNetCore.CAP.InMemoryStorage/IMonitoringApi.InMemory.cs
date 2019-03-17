// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.CAP.Dashboard;
using DotNetCore.CAP.Dashboard.Monitoring;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.InMemoryStorage
{
    internal class InMemoryMonitoringApi : IMonitoringApi
    {
        private readonly IStorage _storage;

        public InMemoryMonitoringApi(IStorage storage)
        {
            _storage = storage;
        }

        public StatisticsDto GetStatistics()
        {
            var connection = GetConnection();
            var stats = new StatisticsDto
            {
                PublishedSucceeded = connection.PublishedMessages.Count(x => x.StatusName == StatusName.Succeeded),
                ReceivedSucceeded = connection.ReceivedMessages.Count(x => x.StatusName == StatusName.Succeeded),
                PublishedFailed = connection.PublishedMessages.Count(x => x.StatusName == StatusName.Failed),
                ReceivedFailed = connection.ReceivedMessages.Count(x => x.StatusName == StatusName.Failed)
            };
            return stats;
        }

        public IDictionary<DateTime, int> HourlyFailedJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, StatusName.Failed);
        }

        public IDictionary<DateTime, int> HourlySucceededJobs(MessageType type)
        {
            return GetHourlyTimelineStats(type, StatusName.Succeeded);
        }

        public IList<MessageDto> Messages(MessageQueryDto queryDto)
        {
            var connection = GetConnection();
            if (queryDto.MessageType == MessageType.Publish)
            {
                var expression = connection.PublishedMessages.Where(x => true);

                if (!string.IsNullOrEmpty(queryDto.StatusName))
                {
                    expression = expression.Where(x => x.StatusName.ToLower() == queryDto.StatusName);
                }

                if (!string.IsNullOrEmpty(queryDto.Name))
                {
                    expression = expression.Where(x => x.Name == queryDto.Name);
                }

                if (!string.IsNullOrEmpty(queryDto.Content))
                {
                    expression = expression.Where(x => x.Content.Contains(queryDto.Content));
                }

                var offset = queryDto.CurrentPage * queryDto.PageSize;
                var size = queryDto.PageSize;

                return expression.Skip(offset).Take(size).Select(x => new MessageDto()
                {
                    Added = x.Added,
                    Content = x.Content,
                    ExpiresAt = x.ExpiresAt,
                    Id = x.Id,
                    Name = x.Name,
                    Retries = x.Retries,
                    StatusName = x.StatusName
                }).ToList();
            }
            else
            {
                var expression = connection.ReceivedMessages.Where(x => true);

                if (!string.IsNullOrEmpty(queryDto.StatusName))
                {
                    expression = expression.Where(x => x.StatusName.ToLower() == queryDto.StatusName);
                }

                if (!string.IsNullOrEmpty(queryDto.Name))
                {
                    expression = expression.Where(x => x.Name == queryDto.Name);
                }

                if (!string.IsNullOrEmpty(queryDto.Group))
                {
                    expression = expression.Where(x => x.Group == queryDto.Name);
                }

                if (!string.IsNullOrEmpty(queryDto.Content))
                {
                    expression = expression.Where(x => x.Content.Contains(queryDto.Content));
                }

                var offset = queryDto.CurrentPage * queryDto.PageSize;
                var size = queryDto.PageSize;

                return expression.Skip(offset).Take(size).Select(x => new MessageDto()
                {
                    Added = x.Added,
                    Group = x.Group,
                    Version = "N/A",
                    Content = x.Content,
                    ExpiresAt = x.ExpiresAt,
                    Id = x.Id,
                    Name = x.Name,
                    Retries = x.Retries,
                    StatusName = x.StatusName
                }).ToList();
            }
        }

        public int PublishedFailedCount()
        {
            return GetConnection().PublishedMessages.Count(x => x.StatusName == StatusName.Failed);
        }

        public int PublishedSucceededCount()
        {
            return GetConnection().PublishedMessages.Count(x => x.StatusName == StatusName.Succeeded);
        }

        public int ReceivedFailedCount()
        {
            return GetConnection().ReceivedMessages.Count(x => x.StatusName == StatusName.Failed);
        }

        public int ReceivedSucceededCount()
        {
            return GetConnection().ReceivedMessages.Count(x => x.StatusName == StatusName.Succeeded);
        }

        private InMemoryStorageConnection GetConnection()
        {
            return (InMemoryStorageConnection)_storage.GetConnection();
        }

        private Dictionary<DateTime, int> GetHourlyTimelineStats(MessageType type, string statusName)
        {
            var endDate = DateTime.Now;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => x.ToString("yyyy-MM-dd-HH"), x => x);

            var connection = GetConnection();

            Dictionary<string, int> valuesMap;
            if (type == MessageType.Publish)
            {
                valuesMap = connection.PublishedMessages
                    .Where(x => x.StatusName == statusName)
                    .GroupBy(x => x.Added.ToString("yyyy-MM-dd-HH"))
                    .ToDictionary(x => x.Key, x => x.Count());
            }
            else
            {
                valuesMap = connection.ReceivedMessages
                    .Where(x => x.StatusName == statusName)
                    .GroupBy(x => x.Added.ToString("yyyy-MM-dd-HH"))
                    .ToDictionary(x => x.Key, x => x.Count());
            }

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key))
                {
                    valuesMap.Add(key, 0);
                }
            }

            var result = new Dictionary<DateTime, int>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }
            return result;
        }
    }
}