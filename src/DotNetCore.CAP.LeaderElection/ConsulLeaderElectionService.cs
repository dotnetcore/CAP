using Consul;
using DotNetCore.CAP.Internal;
using System;
using System.Threading.Tasks;

namespace DotNetCore.CAP.LeaderElection
{
    public class ConsulLeaderElectionService : ILeaderElectionService,IDisposable
    {
        private readonly IDistributedLock _session;
        private LeaderElectionOptions _leaderElectionOptions;
        public ConsulLeaderElectionService(LeaderElectionOptions leaderElectionOptions)
        {
            _leaderElectionOptions = leaderElectionOptions;
            var consulClient = new ConsulClient(config =>
            {
                config.WaitTime = TimeSpan.FromSeconds(5);
                config.Address = new Uri($"http://{_leaderElectionOptions.ServerHostName}:{_leaderElectionOptions.ServerPort}");
            });
            _session = consulClient.CreateLock(leaderElectionOptions.LockName);            
        }

        public bool IsLeader()
        {
            _session.Acquire();
            return _session.IsHeld;
        }
        public void Dispose()
        {
            Task.WaitAll(
                Task.Run(() =>
                {
                    _session.Release();
                }),
                Task.Run(() =>
                {
                    _session.Destroy();
                }));
        }
    }
}
