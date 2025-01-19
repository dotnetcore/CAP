using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Test.Helpers;

public class TestThreadSafeMessageSender : IMessageSender
{
    private readonly List<MediumMessage> _messagesInOrder = new();

    public Task<OperateResult> SendAsync(MediumMessage message)
    { 
        lock (_messagesInOrder)
        {
            _messagesInOrder.Add(message);
        }
        return Task.FromResult(OperateResult.Success);
    }
    
    public int Count => _messagesInOrder.Count;
    public List<MediumMessage> ReceivedMessages => _messagesInOrder.ToList();
}