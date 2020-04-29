using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DotNetCore.CAP.SqlServer.Test
{
    public static class ConnectionUtil
    {
        private const string ConnectionStringTemplateVariable = "Cap_SqlServer_ConnectionString";

        private const string MasterDatabaseName = "master";
        private const string DefaultDatabaseName = "cap_test";

        private const string DefaultConnectionString =
            @"server=localhost;database=cap_test;user id=sa;password=123123";

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

        public static SqlConnection CreateConnection(string connectionString = null)
        {
            connectionString ??= GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}