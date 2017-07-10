using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFOptions
    {
        public const string DefaultSchema = "cap";

        /// <summary>
        /// Gets or sets the schema to use when creating database objects.
        /// Default is <see cref="DefaultSchema"/>.
        /// </summary>
        public string Schema { get; set; } = DefaultSchema;
    }
}
