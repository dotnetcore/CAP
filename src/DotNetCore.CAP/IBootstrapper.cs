using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents bootstrapping logic. For example, adding initial state to the storage or querying certain entities.
    /// </summary>
    public interface IBootstrapper
    {
        Task BootstrapAsync();
    }
}