using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Oracle
{
    /// <summary>
    /// OracleParameterExtensions
    /// </summary>
    public static class OracleParameterExtensions
    {
        /// <summary>
        /// Bootstrap a OracleParameterName by a to_date function,it woll be used to SQL query compair to a datetime value
        /// </summary>
        /// <param name="parameterOrValue"></param>
        /// <returns></returns>
        public static string BootstrapDateFunction(this string parameterOrValue)
        {
            return $"to_date('{parameterOrValue}' ,'yyyy-MM-dd hh24:mi:ss')";
        }
    }
}
