// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace DotNetCore.CAP.NATS;

public class ConnectionPool : IConnectionPool, IAsyncDisposable
{
    private readonly NATSOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Queue<INatsConnection> _connections = new();
    private readonly object _lock = new();
    private bool _disposed;

    public ConnectionPool(ILoggerFactory loggerFactory, IOptions<NATSOptions> options)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        if (_options.ConnectionPoolSize < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "ConnectionPoolSize must not be negative.");
    }

    public string ServersAddress => _options.Servers;

    public async ValueTask<INatsConnection> RentConnectionAsync()
    {
        INatsConnection? connection;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            connection = _connections.Count > 0 ? _connections.Dequeue() : null;
        }

        connection ??= new NatsConnection((_options.Options ?? NatsOpts.Default) with
        {
            Url = _options.Servers,
            LoggerFactory = _options.Options?.LoggerFactory ?? _loggerFactory,
            PublishTimeoutOnDisconnected = true
        });

        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);
            lock (_lock)
                ObjectDisposedException.ThrowIf(_disposed, this);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask<bool> ReturnAsync(INatsConnection connection)
    {
        lock (_lock)
        {
            if (!_disposed && _connections.Count < _options.ConnectionPoolSize &&
                connection.ConnectionState == NatsConnectionState.Open)
            {
                _connections.Enqueue(connection);
                return true;
            }
        }

        await connection.DisposeAsync().ConfigureAwait(false);
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        INatsConnection[] connections;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            connections = _connections.ToArray();
            _connections.Clear();
        }

        foreach (var connection in connections)
            await connection.DisposeAsync().ConfigureAwait(false);
    }
}
