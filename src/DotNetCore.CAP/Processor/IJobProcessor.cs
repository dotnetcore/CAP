using System.Threading.Tasks;

namespace DotNetCore.CAP.Processor
{
    public interface IJobProcessor
    {
        Task ProcessAsync(ProcessingContext context);
    }
}