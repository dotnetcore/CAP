using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class EFOptions
    {
        /// <summary>
        /// EF db context type.
        /// </summary>
        internal Type DbContextType { get; set; }
    }
}