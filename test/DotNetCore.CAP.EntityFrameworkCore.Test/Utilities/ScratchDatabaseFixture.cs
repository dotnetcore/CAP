using System;
using Microsoft.EntityFrameworkCore.Internal;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class ScratchDatabaseFixture : IDisposable
    {
        private LazyRef<SqlServerTestStore> _testStore;

        public ScratchDatabaseFixture() {
            _testStore = new LazyRef<SqlServerTestStore>(() => SqlServerTestStore.CreateScratch());
        }

        public string ConnectionString => _testStore.Value.Connection.ConnectionString;

        public void Dispose() {
            if (_testStore.HasValue) {
                _testStore.Value?.Dispose();
            }
        }
    }
}