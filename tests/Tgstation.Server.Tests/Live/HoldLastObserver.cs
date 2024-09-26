using System;

namespace Tgstation.Server.Tests.Live
{
	sealed class HoldLastObserver<T> : IObserver<T>
	{
		public bool Completed { get; private set; }

		public Exception LastError { get; private set; }

		public T LastValue { get; private set; }

		public ulong ErrorCount { get; private set; }

		public ulong ResultCount { get; private set; }

		public void OnCompleted()
		{
			Completed = true;
		}

		public void OnError(Exception error)
		{
			++ErrorCount;
			LastError = error;
		}

		public void OnNext(T value)
		{
			++ResultCount;
			LastValue = value;
		}
	}
}
