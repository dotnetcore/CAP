using BenchmarkDotNet.Attributes;
using SkyApm.Tracing;

namespace SkyApm.Benchmark
{
    public class UniqueIdParse
    {
        private static readonly IUniqueIdParser Parser = new UniqueIdParser();
        private static readonly string Id = new UniqueId(long.MaxValue, long.MaxValue, long.MaxValue).ToString();

        [Benchmark]
        public void Parse() => Parser.TryParse(Id, out _);
    }
}
