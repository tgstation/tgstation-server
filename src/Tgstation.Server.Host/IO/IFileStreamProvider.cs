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
		/// Gets the <see cref="Stream"/> for the file to consume. May be called multiple times, though cancelling any may cause all calls to be cancelled. All calls yield the same <see cref="Stream"/> reference.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the uploaded <see cref="Stream"/> of the file on success, <see langword="null"/> if it could not be provided.</returns>
		/// <remarks>The resulting <see cref="Stream"/> is owned by the <see cref="IFileStreamProvider"/> and is short lived unless otherwise specified. It should be buffered if it needs use outside the lifetime of the <see cref="IFileStreamProvider"/>.</remarks>
		Task<Stream> GetResult(CancellationToken cancellationToken);
	}
}
