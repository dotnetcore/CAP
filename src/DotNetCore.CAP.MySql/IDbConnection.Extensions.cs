// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Data;

namespace DotNetCore.CAP.MySql
{
    internal static class DbConnectionExtensions
    {
        public static int ExecuteNonQuery(this IDbConnection connection, string sql, IDbTransaction transaction = null,
            params object[] sqlParams)
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

        public static T ExecuteReader<T>(this IDbConnection connection, string sql, Func<IDataReader, T> readerFunc,
            params object[] sqlParams)
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

            var objValue = command.ExecuteScalar();

            T result = default;
            if (objValue != null)
            {
                var returnType = typeof(T);
                var converter = TypeDescriptor.GetConverter(returnType);
                if (converter.CanConvertFrom(objValue.GetType()))
                {
                    result = (T)converter.ConvertFrom(objValue);
                }
                else
                {
                    result = (T)Convert.ChangeType(objValue, returnType);
                }
            }

            return result;
        }
    }
}