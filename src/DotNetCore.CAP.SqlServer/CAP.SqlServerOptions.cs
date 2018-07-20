// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP
{
    public class SqlServerOptions : EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; }

        internal bool IsSqlServer2008 { get; set; }

        public SqlServerOptions UseSqlServer2008()
        {
            IsSqlServer2008 = true;
            return this;
        }
    }
}