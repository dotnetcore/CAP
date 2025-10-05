using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Test.Helpers;

public class TestThreadSafeMessageSender : IMessageSender
{
    private readonly ConcurrentQueue<MediumMessage> _messagesInOrder = [];

    public Task<OperateResult> SendAsync(MediumMessage message)
    {
        lock (_messagesInOrder)
        {
            _messagesInOrder.Enqueue(message);
        }
        return Task.FromResult(OperateResult.Success);
    }

    public int Count => _messagesInOrder.Count;
    public List<MediumMessage> ReceivedMessages => _messagesInOrder.ToList();
}