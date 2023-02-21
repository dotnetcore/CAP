using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNetCore.CAP.Test.Helpers
{
    public static class TestHostedServiceExtensions
    {
        public static void StartHostedServices(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
                {
                    var hostedServices = serviceProvider.GetRequiredService<IEnumerable<IHostedService>>();
                    foreach (var hostedService in hostedServices)
                    {
                        await hostedService.StartAsync(cancellationToken);
                    }
                }, cancellationToken)
                .GetAwaiter().GetResult();
        }
    }
}