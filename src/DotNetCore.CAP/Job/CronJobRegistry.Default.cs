using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Job
{
    public class DefaultCronJobRegistry : CronJobRegistry
    {
        private readonly CapOptions _options;

        public DefaultCronJobRegistry(IOptions<CapOptions> options)
        {
            _options = options.Value;

            RegisterJob<CapJob>(nameof(DefaultCronJobRegistry), _options.CronExp, RetryBehavior.DefaultRetry);
        }
    }
}