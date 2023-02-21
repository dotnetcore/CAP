using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Test.Helpers
{
    public class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestLogger(ITestOutputHelper outputHelper, string categoryName)
        {
            _outputHelper = outputHelper;
            CategoryName = categoryName;
        }

        public string CategoryName { get; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _outputHelper.WriteLine($"[{logLevel}] {formatter.Invoke(state, exception)}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DisposableAction(state);
        }

        private class DisposableAction : IDisposable
        {
            private readonly object _state;

            public DisposableAction(object state)
            {
                _state = state;
            }

            public void Dispose()
            {
            }
        }
    }
}