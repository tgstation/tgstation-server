using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Interface for asynchronously consuming <see cref="Stream"/>s of files.
	/// </summary>
	public interface IFileStreamProvider : IAsyncDisposable
	{
		/// <summary>
		/// Gets the provided <see cref="Stream"/>. May be called multiple times, though cancelling any may cause all calls to be cancelled. All calls yield the same <see cref="Stream"/> reference.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the provided <see cref="Stream"/>.</returns>
		/// <remarks>The resulting <see cref="Stream"/> is owned by the <see cref="IFileStreamProvider"/> and is short lived unless otherwise specified. It should be buffered if it needs use outside the lifetime of the <see cref="IFileStreamProvider"/>.</remarks>
		ValueTask<Stream> GetResult(CancellationToken cancellationToken);
	}
}
