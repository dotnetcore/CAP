using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class StateChanger : IStateChanger
    {
        public void ChangeState(CapPublishedMessage message, IState state, IStorageTransaction transaction)
        {
            var now = DateTime.Now;
            if (state.ExpiresAfter != null)
                message.ExpiresAt = now.Add(state.ExpiresAfter.Value);
            else
                message.ExpiresAt = null;

            message.StatusName = state.Name;
            state.Apply(message, transaction);
            transaction.UpdateMessage(message);
        }

        public void ChangeState(CapReceivedMessage message, IState state, IStorageTransaction transaction)
        {
            var now = DateTime.Now;
            if (state.ExpiresAfter != null)
                message.ExpiresAt = now.Add(state.ExpiresAfter.Value);
            else
                message.ExpiresAt = null;

            message.StatusName = state.Name;
            state.Apply(message, transaction);
            transaction.UpdateMessage(message);
        }
    }
}