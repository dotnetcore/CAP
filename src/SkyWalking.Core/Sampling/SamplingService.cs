using System.Threading.Tasks;

namespace SkyWalking.Sampling
{
    public class SamplingService : ISampler
    {
        public Task Executing()
        {
            return Task.CompletedTask;
        }

        public Task Executed()
        {
            return Task.CompletedTask;
        }

        public bool TrySampling()
        {
            return true;
        }

        public void ForceSampled()
        {
        }
    }
}