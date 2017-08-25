using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class EnqueuedJobDto
    {
        public EnqueuedJobDto()
        {
            InEnqueuedState = true;
        }

        public Message Message { get; set; }
        public string State { get; set; }
        public DateTime? EnqueuedAt { get; set; }
        public bool InEnqueuedState { get; set; }
    }
}