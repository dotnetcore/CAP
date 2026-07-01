using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.GaussDB.Test;

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }
}
