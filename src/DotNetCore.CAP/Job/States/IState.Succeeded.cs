using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Job.States
{
    public class SucceededState : IState
    {
        public const string StateName = "Succeeded";

        public TimeSpan? ExpiresAfter => TimeSpan.FromHours(1);

        public string Name => StateName;

        public void Apply(CapSentMessage message, IStorageTransaction transaction)
        {
        }

        public void Apply(CapReceivedMessage message, IStorageTransaction transaction)
        {
        }
    }
}