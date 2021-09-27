using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotNetCore.CAP.Test.Helpers
{
    public static class ObservableCollectionExtensions
    {
        public static async Task WaitOneMessage<T>(this ObservableCollection<T> collection,
            CancellationToken cancellationToken)
        {
            await WaitForMessages(collection,
                x => x.Count() == 1,
                cancellationToken);
        }

        public static async Task WaitForMessages<T>(this ObservableCollection<T> collection, 
            Func<IEnumerable<T>, bool> comparison,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cts = new CancellationTokenSource();
            cancellationToken.Register(() => cts.Cancel());

            await Task.Run(async () =>
            {
                void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
                {
                    if (comparison(collection))
                    {
                        cts.Cancel();
                    }
                }

                collection.CollectionChanged += OnCollectionChanged;
                
                if (collection.Count > 0)
                {
                    OnCollectionChanged(collection, default);
                }

                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (TaskCanceledException)
                {
                }
                finally
                {
                    collection.CollectionChanged -= OnCollectionChanged;
                }

                cancellationToken.ThrowIfCancellationRequested();

            }, cancellationToken);
        }
    }
}