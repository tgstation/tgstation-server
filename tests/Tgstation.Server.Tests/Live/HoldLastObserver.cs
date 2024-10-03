using System;
using System.Threading;

namespace Tgstation.Server.Tests.Live
{
	sealed class HoldLastObserver<T> : IObserver<T>
	{
		public bool Completed { get; private set; }

		public Exception LastError { get; private set; }

		public T LastValue { get; private set; }

		public ulong ErrorCount => errorCount;

		public ulong ResultCount => resultCount;

		ulong errorCount;
		ulong resultCount;

		public void OnCompleted()
		{
			Completed = true;
		}

		public void OnError(Exception error)
		{
			Interlocked.Increment(ref errorCount);
			LastError = error;
		}

		public void OnNext(T value)
		{
			Interlocked.Increment(ref resultCount);
			LastValue = value;
		}
	}
}
