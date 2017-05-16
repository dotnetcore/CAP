using Cap.Consistency;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class ConsistencyOptions
    {
        public string BrokerUrlList { get; set; } = "localhost:9092";
    }
}