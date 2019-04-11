using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace SkyApm.Benchmark
{
    class Program
    {
        public static void Main(params string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args, ManualConfig
                    .Create(DefaultConfig.Instance)
                    .With(MemoryDiagnoser.Default)
                    .With(ExecutionValidator.FailOnError));
        }
    }
}
