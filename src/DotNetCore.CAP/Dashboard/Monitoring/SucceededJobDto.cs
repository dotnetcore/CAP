using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class SucceededJobDto
    {
        public SucceededJobDto()
        {
            InSucceededState = true;
        }

        public Message Message { get; set; }
        public object Result { get; set; }
        public long? TotalDuration { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}