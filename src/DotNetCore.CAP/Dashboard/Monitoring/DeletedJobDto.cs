using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class DeletedJobDto
    {
        public DeletedJobDto()
        {
            InDeletedState = true;
        }

        public Message Message { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool InDeletedState { get; set; }
    }
}
