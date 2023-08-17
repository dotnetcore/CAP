using DotNetCore.CAP;
using DotNetCore.CAP.Persistence;
using Google.Cloud.Spanner.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Spanner
{
    internal class GoogleSpannerStorageInitializer : IStorageInitializer
    {
        private readonly ILogger logger;
        private readonly IOptions<GoogleSpannerOptions> options;
        private readonly IOptions<CapOptions> capOptions;

        public GoogleSpannerStorageInitializer(
            ILogger<GoogleSpannerStorageInitializer> logger,
            IOptions<GoogleSpannerOptions> options,
            IOptions<CapOptions> capOptions)
        {
            this.options = options;
            this.capOptions = capOptions;
            this.logger = logger;
        }

        public string GetLockTableName()
        {
            return $"{options.Value.Schema}_lock";
        }

        public virtual string GetPublishedTableName()
        {
            return $"{options.Value.Schema}_published";
        }

        public virtual string GetReceivedTableName()
        {
            return $"{options.Value.Schema}_received";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sqls = CreateDbTablesScript(options.Value.Schema);
            using (var connection = new SpannerConnection(options.Value.ConnectionString))
            {
                try
                {
                    if (!await this.TableExists(connection))
                    {
                        foreach (var table in sqls)
                        {
                            var cmd = connection.CreateDdlCommand(table);
                            await cmd.ExecuteNonQueryAsync()
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (SpannerException e)
                {
                    logger.LogError(e, "Error Initializing Spanner Database");
                    throw e;
                }
                await this.InitLocks(connection);
            }

            logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected async Task<bool> TableExists(SpannerConnection connection)
        {
            try
            {
                var cmd = connection.CreateSelectCommand($@"SELECT Count(*) > 0 FROM INFORMATION_SCHEMA.tables
                                                            WHERE TABLE_NAME in ('{GetLockTableName()}','{GetPublishedTableName()}','{GetReceivedTableName()}')");

                return await cmd.ExecuteScalarAsync<bool>()
                    .ConfigureAwait(false);
            }
            catch (SpannerException e)
            {
                logger.LogError($"{e.Message}");
            }

            return false;
        }


        protected async Task InitLocks(SpannerConnection connection)
        {
            try
            {
                var cmd = connection.CreateInsertCommand(GetLockTableName(), new SpannerParameterCollection()
                {
                    { "Key", SpannerDbType.String, $"publish_retry_{capOptions.Value.Version}"},
                    { "Instance", SpannerDbType.String, ""},
                    { "LastLockTime" , SpannerDbType.Timestamp, DateTime.MinValue}
                });
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                //lock exists ignore exception
            }

            try
            {
                var cmd = connection.CreateInsertCommand(GetLockTableName(), new SpannerParameterCollection()
                {
                    { "Key", SpannerDbType.String, $"received_retry_{capOptions.Value.Version}"},
                    { "Instance", SpannerDbType.String, "Instance"},
                    { "LastLockTime" , SpannerDbType.Timestamp, SpannerParameter.CommitTimestamp}
                });
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                //lock exists ignore exception
            }
        }

        protected virtual List<string> CreateDbTablesScript(string schema)
        {
            //CREATE SCHEMA IF NOT EXISTS ""{schema}"";
            var ddls = new List<string> { $@"
                   CREATE TABLE IF NOT EXISTS {GetLockTableName()} (
                      Key STRING(128) NOT NULL,
                      Instance STRING(256),
                      LastLockTime TIMESTAMP NOT NULL,
                    ) PRIMARY KEY(Key)",
                $@"CREATE TABLE IF NOT EXISTS {GetPublishedTableName()} (
                      Id STRING(50) NOT NULL,
                      Version STRING(20) NOT NULL,
                      Name STRING(200) NOT NULL,
                      Content STRING(MAX) NOT NULL,
                      Retries INT64 NOT NULL,
                      Added TIMESTAMP NOT NULL,
                      ExpiresAt TIMESTAMP,
                      StatusName STRING(50) NOT NULL,
                ) PRIMARY KEY(Id)",
                $@"CREATE TABLE IF NOT EXISTS {GetReceivedTableName()} (
                      Id STRING(50) NOT NULL,
                      Version STRING(20) NOT NULL,
                      Name STRING(200) NOT NULL,
                      `Group` STRING(200),
                      Content STRING(MAX),
                      Retries INT64 NOT NULL,
                      Added TIMESTAMP NOT NULL,
                      ExpiresAt TIMESTAMP,
                      StatusName STRING(50) NOT NULL,
                    ) PRIMARY KEY(Id)" };
            return ddls;
        }
    }
}
