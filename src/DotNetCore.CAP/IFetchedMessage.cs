using System;

namespace DotNetCore.CAP
{
	public interface IFetchedMessage : IDisposable
	{
		int MessageId { get; }

		void RemoveFromQueue();

		void Requeue();
	}
}
