using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IQueueExecutorFactory
    {
        IQueueExecutor GetInstance(MessageType messageType);
    }
}