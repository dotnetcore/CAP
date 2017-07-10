using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.EntityFrameworkCore
{
	public class EFStorageTransaction : IStorageTransaction, IDisposable
	{
		private EFStorageConnection _connection;

		public EFStorageTransaction(EFStorageConnection connection)
		{
			_connection = connection;
		}

		public void UpdateJob(Job job)
		{
			if (job == null) throw new ArgumentNullException(nameof(job));

			// NOOP. EF will detect changes.
		}

		public void EnqueueJob(Job job)
		{
			
		}

		public Task CommitAsync()
		{
			return _connection.Context.SaveChangesAsync();
		}

		public void Dispose()
		{
		}

        public void UpdateMessage(CapSentMessage message)
        {
            throw new NotImplementedException();
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            throw new NotImplementedException();
        }

        public void EnqueueMessage(CapSentMessage message)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _connection.Context.Add(new JobQueue
            {
                JobId = job.Id
            });
        }

        public void EnqueueMessage(CapReceivedMessage message)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _connection.Context.Add(new JobQueue
            {
                JobId = job.Id
            });
        }
    }
}
