using System;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql.Test
{
    public static class ConnectionUtil
    {
        private const string ConnectionStringTemplateVariable = "Cap_MySql_ConnectionString";

        private const string MasterDatabaseName = "information_schema";
        private const string DefaultDatabaseName = "cap_test";

        private const string DefaultConnectionString =
            @"Server=localhost;Database=cap_test;Uid=root;Pwd=123123;Allow User Variables=True;SslMode=none;";

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

        public static MySqlConnection CreateConnection(string connectionString = null)
        {
            connectionString ??= GetConnectionString();
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}