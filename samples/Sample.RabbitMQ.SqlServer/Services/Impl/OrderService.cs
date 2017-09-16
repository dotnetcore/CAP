using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP;

namespace Sample.RabbitMQ.SqlServer.Services.Impl
{
    public class OrderService : IOrderService, ICapSubscribe
    {
        [CapSubscribe("sample.rabbitmq.sqlserver.order.check")]
        public void Check()
        {
            Console.WriteLine("out");
        }
    }
}
