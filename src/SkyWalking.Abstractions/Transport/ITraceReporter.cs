using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking.Transport
{
    public interface ITraceReporter
    {
        Task ReportAsync(IReadOnlyCollection<TraceSegmentRequest> segmentRequests,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}