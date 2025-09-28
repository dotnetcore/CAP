﻿using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using System.Data.Common;

namespace Sample.Kafka.PostgreSql
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Age:{Age}";
        }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<Person> Persons { get; set; }
    }

#pragma warning disable EF1001 // Internal EF Core API usage.

    public class CapNpgsqlRelationalConnection : NpgsqlRelationalConnection
    {
        private readonly ICapPublisher _cap;

        protected CapNpgsqlRelationalConnection(RelationalConnectionDependencies dependencies, DbDataSource dataSource) : base(dependencies, dataSource)
        {
            _cap = dependencies.CurrentContext.Context.GetService<ICapPublisher>();
        }

#pragma warning restore EF1001

        public override void CommitTransaction()
        {
            if (_cap.Transaction != null)
            {
                _cap.Transaction.Commit();
            }
            else
            {
                base.CommitTransaction();
            }
        }

        public override Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_cap.Transaction != null)
            {
                return _cap.Transaction.CommitAsync(cancellationToken);
            }
            else
            {
                return base.CommitTransactionAsync(cancellationToken);
            }
        }

        public override void RollbackTransaction()
        {
            if (_cap.Transaction != null)
            {
                _cap.Transaction.Rollback();
            }
            else
            {
                base.RollbackTransaction();
            }
        }

        public override Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_cap.Transaction != null)
            {
                return _cap.Transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                return base.RollbackTransactionAsync(cancellationToken);
            }
        }
    }
}
