using System.Data;
using DotNetCore.CAP.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer.EntityFramework
{
    public class EntityFrameworkSqlServerDataStorage : SqlServerDataStorage
    {
        public EntityFrameworkSqlServerDataStorage(IOptions<CapOptions> capOptions, IOptions<SqlServerOptions> options,
            IStorageInitializer initializer)
            : base(capOptions, options, initializer)
        {
        }

        protected override IDbTransaction GetDbTransaction(object dbTransaction)
        {
            var dbTrans = dbTransaction as IDbTransaction;
            if (dbTrans == null && dbTransaction is IDbContextTransaction dbContextTrans)
                dbTrans = dbContextTrans.GetDbTransaction();

            return dbTrans;
        }
    }
}