// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace DotNetCore.CAP.Oracle
{
    public class OracleStorageInitializer : IStorageInitializer
    {
        private readonly IOptions<OracleOptions> _options;
        private readonly ILogger _logger;

        public OracleStorageInitializer(
            ILogger<OracleStorageInitializer> logger,
            IOptions<OracleOptions> options)
        {
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $"\"{_options.Value.GetUserName()}\".\"{_options.Value.TableNamePrefix}_published\"";
        }

        public virtual string GetReceivedTableName()
        {
            return $"\"{_options.Value.GetUserName()}\".\"{_options.Value.TableNamePrefix}_received\"";
        }

        private string GetOrginalTableName(string fullTableName)
        {
            return fullTableName.Replace($"\"{_options.Value.GetUserName()}\"", string.Empty).Trim('"');
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var sql = CreateDbTablesScript();
            using (var connection = new OracleConnection(_options.Value.ConnectionString))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        /// <summary>
        /// Get the sql caluse string will to create published table and received table .
        /// </summary>
        /// <returns></returns>
        protected virtual string CreateDbTablesScript()
        {
            var batchSql =
                $@"
                        declare tableRecExists integer;
                        begin
                        select count(1) into tableRecExists from user_tables where table_name ='{GetOrginalTableName(GetReceivedTableName())}';
                        if tableRecExists=0 then
                            execute immediate'
                            CREATE TABLE {GetReceivedTableName()} (
                                   ""Id"" number(23,0) NOT NULL,
                                   ""Version"" varchar2(20) DEFAULT NULL,
                                   ""Name"" varchar2(400) NOT NULL,
                                   ""Group"" varchar2(200) DEFAULT NULL,
                                   ""Content"" clob,
                                   ""Retries"" number(11,0) DEFAULT NULL,
                                   ""Added"" date NOT NULL,
                                   ""ExpiresAt"" date DEFAULT NULL,
                                   ""StatusName"" varchar2(50) NOT NULL
                                )';
                               execute immediate 'ALTER TABLE ""{GetOrginalTableName(GetReceivedTableName())}"" ADD CONSTRAINT ""PK_Received"" PRIMARY KEY (""Id"")';
                               execute immediate 'CREATE INDEX ""IX_ExpiresAt"" ON {GetOrginalTableName(GetReceivedTableName())}(""ExpiresAt"")';
                            end if;
                        end;

                        declare tablePubExists integer;
                        begin
                        select count(*) into tablePubExists from user_tables where table_name ='{GetOrginalTableName(GetPublishedTableName())}';
                        if tablePubExists=0 then
                            execute immediate'
                            CREATE TABLE {GetPublishedTableName()} (
                                 ""Id"" number(23,0) NOT NULL,
                                 ""Version"" varchar2(20) DEFAULT NULL,
                                 ""Name"" varchar2(200) NOT NULL,
                                 ""Content"" clob,
                                 ""Retries"" number(11,0) DEFAULT NULL,
                                 ""Added"" date NOT NULL,
                                 ""ExpiresAt"" date DEFAULT NULL,
                                 ""StatusName"" varchar2(50) NOT NULL
                                )';
                                execute immediate 'ALTER TABLE ""{GetOrginalTableName(GetPublishedTableName())}"" ADD CONSTRAINT ""PK_Published"" PRIMARY KEY (""Id"")';
                                execute immediate 'CREATE INDEX ""IX_ExpiresAt"" ON {GetOrginalTableName(GetPublishedTableName())}(""ExpiresAt"")';
                            end if;
                        end;
            ";
            return batchSql;
        }
    }
}