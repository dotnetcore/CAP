using System;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Abstractions
{
    public interface IMongoTransaction : IDisposable
    {
        /// <summary>
        /// If set true, the session.CommitTransaction() will be called automatically.
        /// </summary>
        /// <value></value>
        bool AutoCommit { get; set; }

        Task<IMongoTransaction> BegeinAsync(bool autoCommit = true);
        
        IMongoTransaction Begein(bool autoCommit = true);
    }
}