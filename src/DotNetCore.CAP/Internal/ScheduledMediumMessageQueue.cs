using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Internal;

public class ScheduledMediumMessageQueue
{
    private readonly SortedSet<(long, MediumMessage)> _queue = new(Comparer<(long, MediumMessage)>.Create((a, b) =>
    {
        int result = a.Item1.CompareTo(b.Item1);
        return result == 0 ? String.Compare(a.Item2.DbId, b.Item2.DbId, StringComparison.Ordinal) : result;
    }));

    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly object _lock = new();

    public void Enqueue(MediumMessage message, long sendTime)
    {
        lock (_lock)
        {
            _queue.Add((sendTime, message));
        }
        
        _semaphore.Release();
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
    
    public IEnumerable<MediumMessage> UnorderedItems
    {
        get
        {
            lock (_lock)
            {
                return _queue.Select(x => x.Item2).ToList();
            }
        }
    }
    
    public async IAsyncEnumerable<MediumMessage> GetConsumingEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(cancellationToken);

            (long, MediumMessage)? nextItem = null;

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    var topMessage = _queue.First();
                    var timeLeft = topMessage.Item1 - DateTime.Now.Ticks;
                    if (timeLeft < 500000) // 50ms
                    {
                        nextItem = topMessage;
                        _queue.Remove(topMessage);
                    }
                }
            }

            if (nextItem is not null)
            {
                yield return nextItem.Value.Item2;
            }
            else
            {
                // Re-release the semaphore if no item is ready yet
                _semaphore.Release();
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}