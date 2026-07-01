using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

[Collection(GaussDBCollection.Name)]
public class GaussDBCompatibilityTests
{
    [Fact]
    public async Task ServerMeetsRequiredCompatibilityVersions()
    {
        if (!ConnectionUtil.IsConnectionAvailable) return;

        await using var connection = ConnectionUtil.CreateConnection();
        await connection.OpenAsync();

        await using var serverCommand = connection.CreateCommand();
        serverCommand.CommandText = "SHOW server_version";
        var serverVersion = ParseVersion(Assert.IsType<string>(await serverCommand.ExecuteScalarAsync()));

        await using var openGaussCommand = connection.CreateCommand();
        openGaussCommand.CommandText = "SELECT opengauss_version()";
        var openGaussVersion = ParseVersion(Assert.IsType<string>(await openGaussCommand.ExecuteScalarAsync()));

        Assert.True(serverVersion >= new Version(9, 2, 4), $"server_version was {serverVersion}");
        Assert.True(openGaussVersion >= new Version(3, 0, 0), $"openGauss version was {openGaussVersion}");
    }

    private static Version ParseVersion(string value)
    {
        var match = Regex.Match(value, @"\d+\.\d+(?:\.\d+)?");
        Assert.True(match.Success, $"No version found in: {value}");
        return Version.Parse(match.Value);
    }
}
