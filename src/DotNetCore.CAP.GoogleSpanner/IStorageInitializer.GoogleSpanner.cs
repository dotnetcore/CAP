using DotNetCore.CAP.Persistence;
using Google.Cloud.Spanner.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.GoogleSpanner
{
    internal class GoogleSpannerStorageInitializer : IStorageInitializer
    {
        private readonly ILogger _logger;
        private readonly IOptions<GoogleSpannerOptions> _options;

        public GoogleSpannerStorageInitializer(
            ILogger<GoogleSpannerStorageInitializer> logger,
            IOptions<GoogleSpannerOptions> options)
        {
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $"{_options.Value.Schema}_published";
        }

        public virtual string GetReceivedTableName()
        {
            return $"{_options.Value.Schema}_received";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var sql = CreatePublishedTableScript(_options.Value.Schema);
            using (var connection = new SpannerConnection(_options.Value.ConnectionString))
            {
                try
                {
                    var cmd = connection.CreateDdlCommand(sql);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SpannerException e) when (e.ErrorCode == ErrorCode.FailedPrecondition)
                {
                    // Table already exist.  Not a problem.
                }
            }

            sql = CreateReceivedTableScript(_options.Value.Schema);
            using (var connection = new SpannerConnection(_options.Value.ConnectionString))
            {
                try
                {
                    var cmd = connection.CreateDdlCommand(sql);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SpannerException e) when (e.ErrorCode == ErrorCode.FailedPrecondition)
                {
                    // Table already exist.  Not a problem.
                }
            }

            await Task.CompletedTask;

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }

        protected virtual string CreatePublishedTableScript(string schema)
        {
            //CREATE SCHEMA IF NOT EXISTS ""{schema}"";
            var batchSql = $@"
            CREATE TABLE {GetPublishedTableName()} (
	            Id STRING(50) NOT NULL,
                Version STRING(20),
	            Name STRING(50),
	            Content STRING(MAX),
	            Retries INT64 NOT NULL,
	            Added TIMESTAMP NOT NULL,
                ExpiresAt TIMESTAMP,
	            StatusName STRING(50)
            )PRIMARY KEY (Id)";
            return batchSql;
        }

        protected virtual string CreateReceivedTableScript(string schema)
        {
            //CREATE SCHEMA IF NOT EXISTS ""{schema}"";
            var batchSql = $@"
                CREATE TABLE {GetReceivedTableName()} (
	                Id STRING(50) NOT NULL,
                    Version STRING(20),
	                Name STRING(50),
	                GroupName STRING(50),
	                Content STRING(MAX),
	                Retries INT64 NOT NULL,
	                Added TIMESTAMP NOT NULL,
                    ExpiresAt TIMESTAMP,
	                StatusName STRING(50)
                )PRIMARY KEY (Id)";
            return batchSql;
        }
    }
}