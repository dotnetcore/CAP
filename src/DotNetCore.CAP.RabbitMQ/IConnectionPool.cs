using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public interface IConnectionPool
    {
        IConnection Rent();

        bool Return(IConnection context);
    }
}
