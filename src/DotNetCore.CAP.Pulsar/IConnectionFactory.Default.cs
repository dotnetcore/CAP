// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Pulsar.Client.Api;

namespace DotNetCore.CAP.Pulsar
{
    public class ConnectionFactory : IConnectionFactory, IAsyncDisposable
    {
        private readonly ILogger<ConnectionFactory> _logger;
        private PulsarClient? _client;
        private readonly PulsarOptions _options;
        private readonly ConcurrentDictionary<string, Task<IProducer<byte[]>>> _topicProducers;

        public ConnectionFactory(ILogger<ConnectionFactory> logger, IOptions<PulsarOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _topicProducers = new ConcurrentDictionary<string, Task<IProducer<byte[]>>>();

            logger.LogDebug("CAP Pulsar configuration: {0}", JsonConvert.SerializeObject(_options, Formatting.Indented));
        }

        public string ServersAddress => _options.ServiceUrl;

        public async Task<IProducer<byte[]>> CreateProducerAsync(string topic)
        {
            _client ??= RentClient();

            async Task<IProducer<byte[]>> ValueFactory(string top)
            {
                return await _client.NewProducer()
                    .Topic(top)
                    .CreateAsync();
            }

            //connection may lost
            return await _topicProducers.GetOrAdd(topic, ValueFactory);
        }

        public PulsarClient RentClient()
        {
            lock (this)
            {
                if (_client == null)
                {
                    var builder = new PulsarClientBuilder().ServiceUrl(_options.ServiceUrl);
                    if (_options.TlsOptions != null)
                    {
                        builder.EnableTls(_options.TlsOptions.UseTls);
                        builder.EnableTlsHostnameVerification(_options.TlsOptions.TlsHostnameVerificationEnable);
                        builder.AllowTlsInsecureConnection(_options.TlsOptions.TlsAllowInsecureConnection);
                        builder.TlsTrustCertificate(_options.TlsOptions.TlsTrustCertificate);
                        builder.Authentication(_options.TlsOptions.Authentication);
                        builder.TlsProtocols(_options.TlsOptions.TlsProtocols);
                    }

                    _client = builder.BuildAsync().Result;
                }

                return _client;
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var value in _topicProducers.Values)
            {
                _ = (await value).DisposeAsync();
            }
        }
    }
}