using System;
using System.IO;
using Xunit;

namespace DotNetCore.CAP.GaussDB.Test;

public class DependencyIsolationTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string ProjectDirectory = Path.Combine(
        RepositoryRoot, "src", "DotNetCore.CAP.GaussDB");

    [Fact]
    public void ProductionProject_HasNoNpgsqlOrPostgreSqlReference()
    {
        Assert.True(Directory.Exists(ProjectDirectory), $"Missing production project: {ProjectDirectory}");

        var sourceFiles = Directory.GetFiles(ProjectDirectory, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sourceFiles);

        var projectPath = Path.Combine(ProjectDirectory, "DotNetCore.CAP.GaussDB.csproj");
        var project = File.ReadAllText(projectPath);
        Assert.DoesNotContain("Npgsql", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DotNetCore.CAP.PostgreSql", project, StringComparison.OrdinalIgnoreCase);

        foreach (var sourceFile in sourceFiles)
        {
            var source = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("Npgsql", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DotNetCore.CAP.PostgreSql", source, StringComparison.OrdinalIgnoreCase);
        }
    }
}
