using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.Helpers
{
    public class TestLoggingProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestLoggingProvider(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_outputHelper, categoryName);
        }
    }
}