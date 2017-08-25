using System;
using System.Collections.Generic;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class JobDetailsDto
    {
        public Message Message { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public IList<StateHistoryDto> History { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}
