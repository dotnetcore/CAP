using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking
{
    public interface ISkyWalkingAgentStartup
    {
        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}