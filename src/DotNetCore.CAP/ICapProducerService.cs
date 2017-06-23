using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public interface ICapProducerService
    {
        Task SendAsync(string topic, string content);
    }
}