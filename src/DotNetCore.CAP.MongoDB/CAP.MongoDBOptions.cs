// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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

        public string ReceivedCollection { get; } = "Received";

        public string PublishedCollection { get; } = "Published";
    }
}