using System;
using DotNetCore.CAP;

namespace Sample.RabbitMQ.MySql
{
    public interface IKeyedService : ICapSubscribe
    {
        void TestSubscribe();
    }

    public class FooKeyedService : IKeyedService
    {
        [CapSubscribe("sample.rabbitmq.test")]
        public void TestSubscribe()
        {
            Console.WriteLine("Foo Test Subscribe");
        }

        [CapSubscribe("sample.rabbitmq.test2")]
        public void TestSubscribe2()
        {
            Console.WriteLine("Foo Test2 Subscribe");
        }
    }

    public class BarKeyedService : IKeyedService
    {
        [CapSubscribe("sample.rabbitmq.test")]
        public void TestSubscribe()
        {
            Console.WriteLine("Bar Test Subscribe");
        }

        [CapSubscribe("sample.rabbitmq.test", Group = "group2")]
        public void TestSubscribe2()
        {
            Console.WriteLine("Group2, Bar Test Subscribe");
        }
    }
}
