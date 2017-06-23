using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public interface IConsumerClientFactory
    {
        IConsumerClient Create(string groupId, string clientHostAddress);
    }
}
