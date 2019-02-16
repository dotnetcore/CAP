using SkyApm.Tracing;

namespace SkyApm.Sample.Backend.Sampling
{
    public class CustomSamplingInterceptor : ISamplingInterceptor
    {
        public int Priority { get; } = 0;
        
        public bool Invoke(SamplingContext samplingContext, Sampler next)
        {
            return next(samplingContext);
        }
    }
}