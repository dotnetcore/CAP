﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class EFOptions
    {
        public const string DefaultSchema = "cap";

        /// <summary>
        /// Gets or sets the schema to use when creating database objects.
        /// Default is <see cref="DefaultSchema" />.
        /// </summary>
        public string Schema { get; set; } = DefaultSchema;

        /// <summary>
        /// EF DbContext
        /// </summary>
        internal Type DbContextType { get; set; }

        internal bool IsSqlServer2008 { get; set; }

        /// <summary>
        /// Data version
        /// </summary>
        internal string Version { get; set; }

        public EFOptions UseSqlServer2008()
        {
            IsSqlServer2008 = true;
            return this;
        }
    }
}