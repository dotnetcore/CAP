using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class ScheduledJobDto
    {
        public ScheduledJobDto()
        {
            InScheduledState = true;
        }

        public Message Message { get; set; }
        public DateTime EnqueueAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public bool InScheduledState { get; set; }
    }
}