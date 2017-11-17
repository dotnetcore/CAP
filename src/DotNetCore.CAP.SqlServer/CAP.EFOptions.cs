using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class EFOptions
    {
        public const string DefaultSchema = "Cap";

        /// <summary>
        /// Gets or sets the schema to use when creating database objects.
        /// Default is <see cref="DefaultSchema" />.
        /// </summary>
        public string Schema { get; set; } = DefaultSchema;

        /// <summary>
        /// EF dbcontext type.
        /// </summary>
        internal Type DbContextType { get; set; }
    }
}