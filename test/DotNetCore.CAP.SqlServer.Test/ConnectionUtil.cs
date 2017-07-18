using System;
using System.Data.SqlClient;

namespace DotNetCore.CAP.SqlServer.Test
{
    public static class ConnectionUtil
    {
        private const string DatabaseVariable = "Cap_SqlServer_DatabaseName";
        private const string ConnectionStringTemplateVariable = "Cap_SqlServer_ConnectionStringTemplate";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName = @"DotNetCore.CAP.SqlServer.Test";

        private const string DefaultConnectionStringTemplate =
            @"Server=192.168.2.206;Initial Catalog={0};User Id=sa;Password=123123;MultipleActiveResultSets=True";

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

        public static SqlConnection CreateConnection(string connectionString = null)
        {
            connectionString = connectionString ?? GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}