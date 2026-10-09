// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace DotNetCore.CAP.NATS;

internal class NATSTransport : ITransport
{
    private readonly IConnectionPool _connectionPool;
    private readonly ILogger _logger;

    public NATSTransport(ILogger<NATSTransport> logger, IConnectionPool connectionPool)
    {
        _logger = logger;
        _connectionPool = connectionPool;
    }

    public BrokerAddress BrokerAddress => new BrokerAddress("NATS", _connectionPool.ServersAddress);

    public async Task<OperateResult> SendAsync(TransportMessage message)
    {
        INatsConnection? connection = null;

        try
        {
            connection = await _connectionPool.RentConnectionAsync().ConfigureAwait(false);
            var headers = new NatsHeaders();
            foreach (var header in message.Headers)
            {
                headers[header.Key] = header.Value;
            }

            var js = new NatsJSContext(connection, new NatsJSOpts(connection.Opts,
                requestTimeout: TimeSpan.FromSeconds(3)));
            var resp = await js.PublishAsync(message.GetName(), message.Body,
                serializer: NatsRawSerializer<ReadOnlyMemory<byte>>.Default,
                opts: new NatsJSPubOpts { MsgId = message.GetId() }, headers: headers).ConfigureAwait(false);
            // A duplicate acknowledgment means an earlier CAP retry was already stored.
            // EnsureSuccess() also rejects duplicates, which would keep CAP retrying forever.
            if (resp.Error != null)
                throw new NatsJSApiException(resp.Error);

            if (resp.Seq > 0)
            {
                _logger.LogDebug("NATS stream message [{MessageName}] has been published.", message.GetName());

                return OperateResult.Success;
            }

            throw new PublisherSentFailedException("NATS message send failed, no stream acknowledgment received.");
        }
        catch (Exception ex)
        {
            var warpEx = new PublisherSentFailedException(ex.Message, ex);

            return OperateResult.Failed(warpEx);
        }
        finally
        {
            if (connection != null)
                await _connectionPool.ReturnAsync(connection).ConfigureAwait(false);
        }
    }
}
