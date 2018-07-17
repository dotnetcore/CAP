using System;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBOptions
    {
        public const string DefaultDatabase = "Cap";

        /// <summary>
        /// Gets or sets the database to use when creating database objects.
        /// Default is <see cref="DefaultDatabase" />.
        /// </summary>
        public string Database { get; set; } = DefaultDatabase;
    }
}