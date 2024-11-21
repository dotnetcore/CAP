using System;
using System.Threading.Tasks;
using DotNetCore.CAP;

namespace Sample.ConsoleApp
{
    public class EventSubscriber : ICapSubscribe
    {
        [CapSubscribe("sample.console.showtime")]
        public async Task ShowTime(DateTime date)
        {
            Console.WriteLine(date);
            await Task.CompletedTask;
        }
    }
}
