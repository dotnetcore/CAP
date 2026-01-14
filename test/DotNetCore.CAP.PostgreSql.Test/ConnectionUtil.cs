using Npgsql;
using System;

namespace DotNetCore.CAP.PostgreSql.Test
{
    public static class ConnectionUtil
    {
        private const string ConnectionStringTemplateVariable = "Cap_PostgreSql_ConnectionString";

        private const string MasterDatabaseName = "postgres";
        private const string DefaultDatabaseName = "cap_test";

        private const string DefaultConnectionString = @"Host=localhost;Database=cap_test;Username=postgres;Password=123456";

        public static string GetDatabaseName()
        {
            return DefaultDatabaseName;
        }

        public static string GetMasterConnectionString()
        {
            return GetConnectionString().Replace(DefaultDatabaseName, MasterDatabaseName);
        }

        public static string GetConnectionString()
        {
            return
                Environment.GetEnvironmentVariable(ConnectionStringTemplateVariable) ??
                DefaultConnectionString;
        }

        public static NpgsqlConnection CreateConnection(string connectionString = null)
        {
            connectionString ??= GetConnectionString();
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}