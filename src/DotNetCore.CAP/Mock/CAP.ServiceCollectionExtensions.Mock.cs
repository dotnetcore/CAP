// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using DotNetCore.CAP;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP.Mock
{
    public static class MockServiceCollectionExtensions
    {
        public static CapBuilder AddMockCap(
           this IServiceCollection services)
        {
            services.TryAddSingleton<CapMarkerService>();
            services.TryAddSingleton<CapMessageQueueMakerService>();
            services.TryAddSingleton<CapDatabaseStorageMarkerService>();

            services.AddSingleton<IBootstrapper, MockBootstrapper>();
            services.AddSingleton<ISubscriberExecutor, MockSubscriberExecutor>();
            services.AddSingleton<ICapPublisher, MockCapPublisher>();

            return new CapBuilder(services);
        }
    }
}