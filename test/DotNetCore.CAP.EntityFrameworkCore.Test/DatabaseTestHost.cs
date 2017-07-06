using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public abstract class DatabaseTestHost : TestHost
    {
        private static bool _sqlObjectInstalled;

        protected override void PostBuildServices()
        {
            base.PostBuildServices();
            InitializeDatabase();
        }

        public override void Dispose()
        {
            DeleteAllData();
            base.Dispose();
        }

        private void InitializeDatabase()
        {
            if (!_sqlObjectInstalled)
            {
                using (CreateScope())
                {
                    var context = GetService<TestDbContext>();
                    context.Database.EnsureDeleted();
                    context.Database.Migrate();
                    _sqlObjectInstalled = true;
                }
            }
        }

        private void DeleteAllData()
        {
            using (CreateScope())
            {
                var context = GetService<TestDbContext>();

                var commands = new[]
                {
                    "DISABLE TRIGGER ALL ON ?",
                    "ALTER TABLE ? NOCHECK CONSTRAINT ALL",
                    "DELETE FROM ?",
                    "ALTER TABLE ? CHECK CONSTRAINT ALL",
                    "ENABLE TRIGGER ALL ON ?"
                };
                foreach (var command in commands)
                {
                    context.Database.GetDbConnection().Execute(
                        "sp_MSforeachtable",
                        new {command1 = command},
                        commandType: CommandType.StoredProcedure);
                }
            }
        }
    }
}