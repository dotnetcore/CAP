using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Consumer
{
    public interface IConsumerClientFactory
    {
        IConsumerClient Create(string groupId, string clientHostAddress);
    }
}
