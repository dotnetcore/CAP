using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface IPublishMessageSender
    {
        Task<OperateResult> SendAsync(CapPublishedMessage message);
    }
}