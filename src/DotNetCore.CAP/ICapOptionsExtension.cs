using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    public interface ICapOptionsExtension
    {
        void AddServices(IServiceCollection services);
    }
}