using Cap.Consistency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
