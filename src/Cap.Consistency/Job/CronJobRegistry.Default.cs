using System;
using System.Collections.Generic;
using System.Text;
using Cap.Consistency.Infrastructure;
using Microsoft.Extensions.Options;

namespace Cap.Consistency.Job
{
    public class DefaultCronJobRegistry : CronJobRegistry
    {
        private readonly ConsistencyOptions _options;

        public DefaultCronJobRegistry(IOptions<ConsistencyOptions> options) {
            _options = options.Value;

            RegisterJob<CapJob>(nameof(DefaultCronJobRegistry), _options.CronExp, RetryBehavior.DefaultRetry);
        }
    }
}
