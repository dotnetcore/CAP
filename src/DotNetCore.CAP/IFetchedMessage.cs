using System;

namespace DotNetCore.CAP
{
	public interface IFetchedMessage : IDisposable
	{
		string MessageId { get; }

		void RemoveFromQueue();

		void Requeue();
	}
}
