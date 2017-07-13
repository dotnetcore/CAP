using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
	public interface IFetchedMessage : IDisposable
	{
		string MessageId { get; }

        MessageType Type { get; }

		void RemoveFromQueue();

		void Requeue();
	}
}
