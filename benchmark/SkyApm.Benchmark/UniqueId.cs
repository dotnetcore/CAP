using BenchmarkDotNet.Attributes;
using SkyApm.Tracing;

namespace SkyApm.Benchmark
{
    public class UniqueId
    {
        private static readonly IUniqueIdGenerator Generator = new UniqueIdGenerator(new RuntimeEnvironment());

        [Benchmark]
        public void Generate() => Generator.Generate();
    }
}
