using System;

namespace DotNetCore.CAP
{
    public interface ICapTransaction : IDisposable
    {
        bool AutoCommit { get; set; }

        object DbTransaction { get; set; }

        void Commit();

        void Rollback();
    }
}
