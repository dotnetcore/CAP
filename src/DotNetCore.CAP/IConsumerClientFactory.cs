using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Consumer client factory to create consumer client instance.
    /// </summary>
    public interface IConsumerClientFactory
    {
        /// <summary>
        /// Create a new instance of <see cref="IConsumerClient"/>.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="clientHostAddress"></param>
        /// <returns></returns>
        IConsumerClient Create(string groupId, string clientHostAddress);
    }
}
