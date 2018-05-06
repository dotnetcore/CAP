using System;
using System.Data.Common;
using  SkyWalking.NetworkProtocol.Trace;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public interface IEfCoreSpanMetadataProvider
    {
        IComponent Component { get; }

        bool Match(DbConnection connection);

        string GetPeer(DbConnection connection);
    }
}