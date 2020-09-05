using System;
using DotNetCore.CAP;

namespace Sample.ConsoleApp
{
    public class EventSubscriber : ICapSubscribe
    {
        [CapSubscribe("sample.aws.in-memory", Group = "aaaa")]
        public void ShowTime(Guid date)
        {
            Console.WriteLine(date);
        }
    }
}
