// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Models
{
    public class CapPublishedMessage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CapPublishedMessage" />.
        /// </summary>
        public CapPublishedMessage()
        {
            Added = DateTime.Now;
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public DateTime Added { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int Retries { get; set; }

        public string StatusName { get; set; }

        public override string ToString()
        {
            return "name:" + Name + ", content:" + Content;
        }
    }
}