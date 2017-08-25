using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class FetchedJobDto
    {
        public Message Message { get; set; }
        public string State { get; set; }
        public DateTime? FetchedAt { get; set; }
    }
}
