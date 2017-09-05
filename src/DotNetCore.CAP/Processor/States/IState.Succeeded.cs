using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class SucceededState : IState
    {
        public const string StateName = "Succeeded";

        public TimeSpan? ExpiresAfter { get; private set; }

        public string Name => StateName;

        public SucceededState()
        {
            ExpiresAfter = TimeSpan.FromHours(1);
        }

        public SucceededState(int ExpireAfterSeconds)
        {
            ExpiresAfter = TimeSpan.FromSeconds(ExpireAfterSeconds);
        }

        public void Apply(CapPublishedMessage message, IStorageTransaction transaction)
        {
        }

        public void Apply(CapReceivedMessage message, IStorageTransaction transaction)
        {
        }
    }
}