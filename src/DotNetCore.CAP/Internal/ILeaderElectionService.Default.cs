namespace DotNetCore.CAP.Internal
{
	public class LeaderElectionService : ILeaderElectionService
	{
		public bool IsLeader() => true;
	}
}
