using DotNetCore.CAP;
using System;

namespace Sample.ConsoleApp
{
    public class EventSubscriber : ICapSubscribe
    {
        [CapSubscribe("sample.console.showtime")]
        public void ShowTime(DateTime date)
        {
            Console.WriteLine(date);
        }

        /// <summary>
        /// 延迟队列消费端
        /// </summary>
        /// <param name="senttime">发送时间</param>
        [CapSubscribe("rk.delayed", Group = "queue.delayed.net")]
        public void ShowTime(string senttime)
        {
            Console.WriteLine($"process time: {senttime}");
            Console.WriteLine($"current time: {DateTime.Now}");
        }
    }
}