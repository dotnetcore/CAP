using System;
using System.Collections.Generic;
using System.Text;
using DotNetCore.CAP.EntityFrameworkCore;

namespace DotNetCore.CAP
{
    public class SqlServerOptions : EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; } //= "Server=DESKTOP-M9R8T31;Initial Catalog=Test;User Id=sa;Password=P@ssw0rd;MultipleActiveResultSets=True";

    }
}
