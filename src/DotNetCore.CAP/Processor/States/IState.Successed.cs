using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Processor.States
{
    public class SuccessedState : IState
    {
        public const string StateName = "Successed";

        public TimeSpan? ExpiresAfter { get; private set; }

        public string Name => StateName;

        public SuccessedState()
        {
            ExpiresAfter = TimeSpan.FromHours(1);
        }

        public SuccessedState(int ExpireAfterSeconds)
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