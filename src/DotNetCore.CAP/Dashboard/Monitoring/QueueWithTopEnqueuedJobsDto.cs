namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class QueueWithTopEnqueuedJobsDto
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public long? Fetched { get; set; }
        public JobList<EnqueuedJobDto> FirstJobs { get; set; }
    }
}
