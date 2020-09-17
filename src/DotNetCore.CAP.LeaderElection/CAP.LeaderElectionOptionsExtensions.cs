using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DotNetCore.CAP.LeaderElection
{
    internal sealed class LeaderElectionOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<LeaderElectionOptions> _options;

        public LeaderElectionOptionsExtension(Action<LeaderElectionOptions> option)
        {
            _options = option;
        }

        public void AddServices(IServiceCollection services)
        {
            var leaderElectionOptions = new LeaderElectionOptions();

            _options?.Invoke(leaderElectionOptions);
            services.AddSingleton(leaderElectionOptions);

            services.AddSingleton<ILeaderElectionService>(r=>new ConsulLeaderElectionService(leaderElectionOptions));
        }
    }

    public static class CapLeaderElectionOptionsExtensions
    {
        public static CapOptions UseLeaderElection(this CapOptions capOptions)
        {
            return capOptions.UseLeaderElection(opt => { });
        }

        public static CapOptions UseLeaderElection(this CapOptions capOptions, Action<LeaderElectionOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            capOptions.RegisterExtension(new LeaderElectionOptionsExtension(options));

            return capOptions;
        }
    }
}