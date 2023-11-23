using System;
using System.Threading;
using System.Threading.Tasks;

#nullable disable

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class AsyncDelayer : IAsyncDelayer
	{
		/// <inheritdoc />
		public Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken) => Task.Delay(timeSpan, cancellationToken);
	}
}
