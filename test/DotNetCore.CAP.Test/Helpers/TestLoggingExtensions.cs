using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.Helpers
{
    public static class TestLoggingExtensions
    {
        public static void AddTestLogging(this ILoggingBuilder builder, ITestOutputHelper outputHelper)
        {
            builder.AddProvider(new TestLoggingProvider(outputHelper));
        }
    }
}