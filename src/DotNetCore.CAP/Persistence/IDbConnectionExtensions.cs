using System;
using System.Data;

namespace DotNetCore.CAP.Persistence
{
    public static class IDbConnectionExtensions
    {
        public static int ExecuteNonQuery(this IDbConnection connection, string sql, IDbTransaction transaction = null, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in sqlParams)
            {
                command.Parameters.Add(param);
            }

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            return command.ExecuteNonQuery();
        }

        public static T ExecuteReader<T>(this IDbConnection connection, string sql, Func<IDataReader, T> readerFunc, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in sqlParams)
            {
                command.Parameters.Add(param);
            }

            var reader = command.ExecuteReader();

            T result = default;
            if (readerFunc != null)
            {
                result = readerFunc(reader);
            }

            return result;
        }

        public static T ExecuteScalar<T>(this IDbConnection connection, string sql, params object[] sqlParams)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            foreach (var param in sqlParams)
            {
                command.Parameters.Add(param);
            }

            var obj = command.ExecuteScalar();

            T result = default;
            if (obj != null)
            {
                result = (T)obj;
            }

            return result;
        }
    }
}