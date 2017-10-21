using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    public interface ICallbackMessageSender
    {
        Task SendAsync(string messageId, string topicName, object bodyObj);
    }
}