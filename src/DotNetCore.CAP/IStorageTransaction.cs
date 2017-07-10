using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
	public interface IStorageTransaction : IDisposable
	{
		void UpdateMessage(CapSentMessage message);

		void UpdateMessage(CapReceivedMessage message);

		void EnqueueMessage(CapSentMessage message);

		void EnqueueMessage(CapReceivedMessage message);

		Task CommitAsync();
	}
}
