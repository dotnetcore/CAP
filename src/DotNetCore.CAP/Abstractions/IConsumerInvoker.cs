using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions
{
    public interface IConsumerInvoker
    {
        Task InvokeAsync();
    }
}