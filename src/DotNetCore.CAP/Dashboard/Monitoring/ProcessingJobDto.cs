using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class ProcessingJobDto
    {
        public ProcessingJobDto()
        {
            InProcessingState = true;
        }

        public Message Message { get; set; }
        public bool InProcessingState { get; set; }
        public string ServerId { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}