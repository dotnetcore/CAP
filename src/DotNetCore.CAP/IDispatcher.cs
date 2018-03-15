using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IDispatcher
    {
        void EnqueuToPublish(CapPublishedMessage message);

        void EnqueuToExecute(CapReceivedMessage message);
    }
}