using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.LeaderElection
{
    public class LeaderElectionOptions
    {
        public const string DefaultServerHost = "localhost";
        public const int DefaultServerPort = 8500;

        public const string DefaultLockName = "cap.leaderelection.lock";

        public LeaderElectionOptions()
        {
            ServerHostName = DefaultServerHost;
            ServerPort = DefaultServerPort;

            LockName = DefaultLockName;
        }

        public string ServerHostName { get; set; }
        public int ServerPort { get; set; }
        public string LockName { get; set; }
    }
}
