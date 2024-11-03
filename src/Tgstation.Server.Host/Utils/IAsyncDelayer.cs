using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// For waiting asynchronously.
	/// </summary>
	interface IAsyncDelayer
	{
		/// <summary>
		/// Create a <see cref="Task"/> that completes after a given <paramref name="timeSpan"/>.
		/// </summary>
		/// <param name="timeSpan">The <see cref="TimeSpan"/> that must elapse.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delay(TimeSpan timeSpan, CancellationToken cancellationToken);
	}
}
