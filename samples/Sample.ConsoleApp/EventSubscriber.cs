using System;
using System.Net.Http;
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

            string baseAddress = "http://localhost:8080/";
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromMilliseconds(10)
            };
            //try
            //{
                var s = await client.GetAsync(baseAddress);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //    Console.WriteLine(e.InnerException.Message);
            //}
        }
    }
}
