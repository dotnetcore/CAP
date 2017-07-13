using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public interface IStateChanger
    {
        void ChangeState(CapSentMessage message, IState state, IStorageTransaction transaction);

        void ChangeState(CapReceivedMessage message, IState state, IStorageTransaction transaction);
    }
}