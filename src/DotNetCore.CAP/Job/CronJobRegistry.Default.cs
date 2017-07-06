using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Job
{
    public class DefaultCronJobRegistry : CronJobRegistry
    {
        public DefaultCronJobRegistry(IOptions<CapOptions> options)
        {
            var options1 = options.Value;

            RegisterJob<CapJob>(nameof(DefaultCronJobRegistry), options1.CronExp, RetryBehavior.DefaultRetry);
        }
    }
}