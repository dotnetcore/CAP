using System;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql.Test
{
    public static class ConnectionUtil
    {
        private const string DatabaseVariable = "Cap_PostgreSql_DatabaseName";
        private const string ConnectionStringTemplateVariable = "Cap_PostgreSql_ConnectionStringTemplate";

        private const string MasterDatabaseName = "postgres";
        private const string DefaultDatabaseName = @"DotNetCore.CAP.PostgreSql.Test";

        private const string DefaultConnectionStringTemplate =
            @"Server=localhost;Database={0};UserId=postgres;Password=123123;";

        public static string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable(DatabaseVariable) ?? DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return string.Format(GetConnectionStringTemplate(), MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return string.Format(GetConnectionStringTemplate(), GetDatabaseName());
        }

        private static string GetConnectionStringTemplate()
        {
            return
                Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable) ??
                DefaultConnectionStringTemplate;
        }

        public static NpgsqlConnection CreateConnection(string connectionString = null)
        {
            connectionString = connectionString ?? GetConnectionString();
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}