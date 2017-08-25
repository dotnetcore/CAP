using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class FailedJobDto
    {
        public FailedJobDto()
        {
            InFailedState = true;
        }

        public Message Message { get; set; }
        public string Reason { get; set; }
        public DateTime? FailedAt { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionDetails { get; set; }
        public bool InFailedState { get; set; }
    }
}