using System;

namespace DotNetCore.CAP
{
	public interface IFetchedMessage : IDisposable
	{
		string MessageId { get; }

        int Type { get; }

		void RemoveFromQueue();

		void Requeue();
	}
}
