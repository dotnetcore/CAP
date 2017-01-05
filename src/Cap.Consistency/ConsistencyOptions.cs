using Cap.Consistency;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents all the options you can use to configure the system.
    /// </summary>
    public class ConsistencyOptions
    {

        /// <summary>
        /// Gets or sets the <see cref="BrokerOptions"/> for the consistency system.
        /// </summary>
        public BrokerOptions Broker { get; set; } = new BrokerOptions();

        public long MaxPendingEventNumber { get; set; }

        public int MaxPendingEventNumber32 {
            get {
                if (this.MaxPendingEventNumber < int.MaxValue) {
                    return (int)this.MaxPendingEventNumber;
                }
                return int.MaxValue;
            }
        }
    }
}