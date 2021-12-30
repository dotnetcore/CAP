// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.MongoDB
{
    // ReSharper disable once InconsistentNaming
    public class MongoDBOptions
    {
        /// <summary>
        /// Gets or sets the database name to use when creating database objects.
        /// Default value: "cap"
        /// </summary>
        public string DatabaseName { get; set; } = "cap";

        /// <summary>
        /// MongoDB database connection string.
        /// Default value: "mongodb://localhost:27017"
        /// </summary>
        public string DatabaseConnection { get; set; } = "mongodb://localhost:27017";

        /// <summary>
        /// MongoDB received message collection name.
        /// Default value: "received"
        /// </summary>
        public string ReceivedCollection { get; set; } = "cap.received";

        /// <summary>
        /// MongoDB published message collection name.
        /// Default value: "published"
        /// </summary>
        public string PublishedCollection { get; set; } = "cap.published";

        internal string Version { get; set; } = default!;
    }
}