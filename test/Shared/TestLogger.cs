using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Test
{
    public interface ITestLogger
    {
        IList<string> LogMessages { get; }
    }

    public class TestLogger<TName> : ILogger<TName>, ITestLogger
    {
        public IList<string> LogMessages { get; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
        {
            LogMessages.Add(state?.ToString());
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                LogMessages.Add(state.ToString());
            }
            else
            {
                LogMessages.Add(formatter(state, exception));
            }
        }
    }
}