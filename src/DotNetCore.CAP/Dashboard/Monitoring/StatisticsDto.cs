namespace DotNetCore.CAP.Dashboard.Monitoring
{
    public class StatisticsDto
    {
        public int Servers { get; set; }

        public int PublishedSucceeded { get; set; }
        public int ReceivedSucceeded { get; set; }

        public int PublishedFailed { get; set; }
        public int ReceivedFailed { get; set; }

        public int PublishedProcessing { get; set; }
        public int ReceivedProcessing { get; set; }
    }
}