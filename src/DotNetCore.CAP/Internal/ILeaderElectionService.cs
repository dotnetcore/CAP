namespace DotNetCore.CAP.Internal
{
    public interface ILeaderElectionService
    {
        bool IsLeader();
    }
}
