using System.Data.Common;
using SkyWalking.Components;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public interface IEfCoreSpanMetadataProvider
    {
        IComponent Component { get; }

        bool Match(DbConnection connection);

        string GetPeer(DbConnection connection);
    }
}