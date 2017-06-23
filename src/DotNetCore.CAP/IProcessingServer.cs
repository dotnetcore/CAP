using System;

namespace DotNetCore.CAP
{
    public interface IProcessingServer : IDisposable
    {
        void Start();
    }
}