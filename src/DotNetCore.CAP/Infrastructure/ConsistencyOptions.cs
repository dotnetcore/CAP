using DotNetCore.CAP.Job;

namespace DotNetCore.CAP.Infrastructure
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class ConsistencyOptions
    {
        public string BrokerUrlList { get; set; } = "localhost:9092";

        public string CronExp { get; set; } = Cron.Minutely();

        public int PollingDelay { get; set; } = 8;
    }
}