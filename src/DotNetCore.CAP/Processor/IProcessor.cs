using System.Threading.Tasks;

namespace DotNetCore.CAP.Processor
{
    public interface IProcessor
    {
        Task ProcessAsync(ProcessingContext context);
    }
}