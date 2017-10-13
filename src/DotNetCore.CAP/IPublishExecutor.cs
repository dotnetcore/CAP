using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public interface IPublishExecutor
    {
        Task<OperateResult> PublishAsync(string keyName, string content);
    }
}
