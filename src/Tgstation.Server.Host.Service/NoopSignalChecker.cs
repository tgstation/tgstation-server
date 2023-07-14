using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// No-op <see cref="ISignalChecker"/>.
	/// </summary>
	sealed class NoopSignalChecker : ISignalChecker
	{
		/// <inheritdoc />
		public Task CheckSignals(Func<string, (int, Task)> startChild, CancellationToken cancellationToken)
		{
			startChild(null);
			return Task.CompletedTask;
		}
	}
}
