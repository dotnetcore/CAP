using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking.Transport
{
    public interface ISegmentReporter
    {
        Task ReportAsync(IReadOnlyCollection<SegmentRequest> segmentRequests,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}