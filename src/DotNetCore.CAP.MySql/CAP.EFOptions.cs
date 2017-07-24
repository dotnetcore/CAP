using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class EFOptions
    {
        /// <summary>
        /// EF dbcontext type.
        /// </summary>
        public Type DbContextType { get; internal set; }
    }
}