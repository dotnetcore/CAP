using System;
using System.Threading.Tasks;

namespace Sample.NetFramewrok.Services
{
    public interface ISubscriberService
    {
        /// <summary>
        /// 接收数据
        /// </summary>
        Task SubscriberData(DateTime dateTime);

    }
}
