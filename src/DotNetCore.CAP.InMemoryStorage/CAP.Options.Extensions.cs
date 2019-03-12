// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseInMemoryStorage(this CapOptions options)
        {
            options.RegisterExtension(new InMemoryCapOptionsExtension());
            return options;
        } 
    }
}