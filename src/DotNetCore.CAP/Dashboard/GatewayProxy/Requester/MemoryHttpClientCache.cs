// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace DotNetCore.CAP.Dashboard.GatewayProxy.Requester
{
    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>> _httpClientsCache =
            new ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>>();

        public void Set(string id, IHttpClient client, TimeSpan expirationTime)
        {
            if (_httpClientsCache.TryGetValue(id, out var connectionQueue))
            {
                connectionQueue.Enqueue(client);
            }
            else
            {
                connectionQueue = new ConcurrentQueue<IHttpClient>();
                connectionQueue.Enqueue(client);
                _httpClientsCache.TryAdd(id, connectionQueue);
            }
        }

        public bool Exists(string id)
        {
            return _httpClientsCache.TryGetValue(id, out _);
        }

        public IHttpClient Get(string id)
        {
            IHttpClient client = null;
            if (_httpClientsCache.TryGetValue(id, out var connectionQueue))
            {
                connectionQueue.TryDequeue(out client);
            }

            return client;
        }

        public void Remove(string id)
        {
            _httpClientsCache.TryRemove(id, out _);
        }
    }
}