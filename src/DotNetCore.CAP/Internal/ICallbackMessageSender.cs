using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    internal interface ICallbackMessageSender
    {
        Task SendAsync(string messageId, string topicName, object bodyObj);
    }
}