using System;
using System.Collections.Generic;
using System.Data; 

namespace DotNetCore.CAP.EntityFrameworkCore
{
    static class HelperExtensions
    {
        public static void Execute(this IDbConnection connection, string sql, IDbTransaction transcation = null)
        {
            try
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELELCT 1";
                    if (transcation != null)
                        command.Transaction = transcation;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                connection.Close();
            }
        }


    }
}