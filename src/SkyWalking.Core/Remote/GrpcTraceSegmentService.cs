using System.Threading;
using System.Threading.Tasks;
using SkyWalking.Boot;
using SkyWalking.Context;
using SkyWalking.Context.Trace;
using SkyWalking.NetworkProtocol;

namespace SkyWalking.Remote
{
    public class GrpcTraceSegmentService : IBootService, ITracingContextListener
    {
        public void Dispose()
        {
            TracingContext.ListenerManager.Remove(this);
        }

        public int Order { get; } = 1;

        public Task Initialize(CancellationToken token)
        {
            TracingContext.ListenerManager.Add(this);
            return Task.CompletedTask;
        }

        public async void AfterFinished(ITraceSegment traceSegment)
        {
            var segment = traceSegment.Transform();
            var traceSegmentService =
                new TraceSegmentService.TraceSegmentServiceClient(GrpcChannelManager.Instance.Channel);
            using (var asyncClientStreamingCall = traceSegmentService.collect())
            {
                await asyncClientStreamingCall.RequestStream.WriteAsync(segment);
                await asyncClientStreamingCall.RequestStream.CompleteAsync();
            }
        }
    }
}