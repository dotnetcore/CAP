// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using DotNetCore.CAP.Pulsar;
using Pulsar.Client.Api;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP pulsar project.
    /// </summary>
    public class PulsarOptions
    {
        public string ServiceUrl { get; set; } = default!;

        public bool EnableClientLog { get; set; } = false;

        public TlsOptions? TlsOptions { get; set; }
    }
}

namespace DotNetCore.CAP.Pulsar
{
    public class TlsOptions
    {
        private static readonly PulsarClientConfiguration Default =
            PulsarClientConfiguration.Default;

        public bool UseTls { get; set; } = Default.UseTls;
        public bool TlsHostnameVerificationEnable { get; set; } = Default.TlsHostnameVerificationEnable;
        public bool TlsAllowInsecureConnection { get; set; } = Default.TlsAllowInsecureConnection;
        public X509Certificate2 TlsTrustCertificate { get; set; } = Default.TlsTrustCertificate;
        public Authentication Authentication { get; set; } = Default.Authentication;
        public SslProtocols TlsProtocols { get; set; } = Default.TlsProtocols;
    }
}