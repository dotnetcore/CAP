// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    using Pulsar;

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
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;

    public class TlsOptions
    {
        private static readonly global::Pulsar.Client.Api.PulsarClientConfiguration Default =
            global::Pulsar.Client.Api.PulsarClientConfiguration.Default;

        public bool UseTls { get; set; } = Default.UseTls;
        public bool TlsHostnameVerificationEnable { get; set; } = Default.TlsHostnameVerificationEnable;
        public bool TlsAllowInsecureConnection { get; set; } = Default.TlsAllowInsecureConnection;
        public X509Certificate2 TlsTrustCertificate { get; set; } = Default.TlsTrustCertificate;
        public global::Pulsar.Client.Api.Authentication Authentication { get; set; } = Default.Authentication;
        public SslProtocols TlsProtocols { get; set; } = Default.TlsProtocols;
    }
}