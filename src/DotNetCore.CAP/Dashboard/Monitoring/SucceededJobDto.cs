using System;
using Hangfire.Common;

namespace Hangfire.Storage.Monitoring
{
    public class SucceededJobDto
    {
        public SucceededJobDto()
        {
            InSucceededState = true;
        }

        public Job Job { get; set; }
        public object Result { get; set; }
        public long? TotalDuration { get; set; }
        public DateTime? SucceededAt { get; set; }
        public bool InSucceededState { get; set; }
    }
}