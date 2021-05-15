using System;
namespace DotNetCore.CAP.Internal
{
    public interface IBootstrapperCallback
    {
        void BootStrappingStarted();

        void OnStart();

        void StorageInitStarted();

        void StorageInitFailed(Exception ex);

        void StorageInitSuccess();

        void BootStrappingFailed(Exception ex);

        void OnStop();
    }
}
