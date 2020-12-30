using System;
using System.Threading.Tasks;
using DotNetCore.CAP;

namespace Sample.NetFramewrok.Services.Impl
{
    public class SubscriberService : ISubscriberService, ICapSubscribe
    {
       

        

        [CapSubscribe("sample.console.showtime")]
        public Task SubscriberData(DateTime dateTime)
        {
            Console.WriteLine(dateTime);
            return Task.CompletedTask;
        }

    }
}
