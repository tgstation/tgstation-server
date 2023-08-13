using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// No-op <see cref="ISignalChecker"/>.
	/// </summary>
	public sealed class NoopSignalChecker : ISignalChecker
	{
		/// <inheritdoc />
		public Task CheckSignals(Func<string, (int, Task)> startChild, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(startChild);
			startChild(null);
			return Task.CompletedTask;
		}
	}
}
