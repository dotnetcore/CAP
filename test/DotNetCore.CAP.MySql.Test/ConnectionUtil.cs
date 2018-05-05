using System;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql.Test
{
    public static class ConnectionUtil
    {
        private const string DatabaseVariable = "Cap_MySql_DatabaseName";
        private const string ConnectionStringTemplateVariable = "Cap_MySql_ConnectionStringTemplate";

        private const string MasterDatabaseName = "information_schema";
        private const string DefaultDatabaseName = @"DotNetCore.CAP.MySql.Test";

        private const string DefaultConnectionStringTemplate =
            @"Server=localhost;Database={0};Uid=root;Pwd=123123;Allow User Variables=True;";

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

        public static MySqlConnection CreateConnection(string connectionString = null)
        {
            connectionString = connectionString ?? GetConnectionString();
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}