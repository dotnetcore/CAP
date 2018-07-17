using System;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBOptions
    {
        public const string DefaultCollection = "Cap";

        /// <summary>
        /// Gets or sets the collection to use when creating database objects.
        /// Default is <see cref="DefaultCollection" />.
        /// </summary>
        public string Collection { get; set; } = DefaultCollection;

        /// <summary>
        /// EF dbcontext type.
        /// </summary>
        internal Type DbContextType { get; set; }
    }
}