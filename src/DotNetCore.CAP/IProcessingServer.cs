using System;

namespace DotNetCore.CAP
{
    /// <summary>
    /// A process thread abstract of job process.
    /// </summary>
    public interface IProcessingServer : IDisposable
    {
        void Start();
    }
}