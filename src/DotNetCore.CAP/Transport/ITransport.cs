using System.Threading.Tasks;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Transport
{
    public interface ITransport
    {
        string Address { get; }

        Task<OperateResult> SendAsync(TransportMessage message);
    }
}
