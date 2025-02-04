// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class CapOptionsExtensions
{
    // ReSharper disable once InconsistentNaming
    public static CapOptions UseRabbitMQ(this CapOptions options, string hostName)
    {
        return options.UseRabbitMQ(opt => { opt.HostName = hostName; });
    }

    // ReSharper disable once InconsistentNaming
    public static CapOptions UseRabbitMQ(this CapOptions options, Action<RabbitMQOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        options.RegisterExtension(new RabbitMqCapOptionsExtension(configure));

        return options;
    }
}