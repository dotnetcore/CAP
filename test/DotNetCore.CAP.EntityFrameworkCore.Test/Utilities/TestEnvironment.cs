using System.IO;
using Microsoft.Extensions.Configuration;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class TestEnvironment
    {
        public static IConfiguration Config { get; }

        static TestEnvironment() {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: true)
                .AddJsonFile("config.test.json", optional: true)
                .AddEnvironmentVariables();

            Config = configBuilder.Build();
        }
    }
}