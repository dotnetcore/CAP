using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GaussDBCollection
{
    public const string Name = "GaussDB";
}
