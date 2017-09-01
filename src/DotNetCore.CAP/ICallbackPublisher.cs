using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface ICallbackPublisher
    {
        Task PublishAsync(CapPublishedMessage obj);
    }
}
