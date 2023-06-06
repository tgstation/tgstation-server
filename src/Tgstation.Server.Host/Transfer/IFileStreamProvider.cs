using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Transfer
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the uploaded <see cref="Stream"/> of the file on success, <see langword="null"/> if the file could not be provider.</returns>
		/// <remarks>The resulting <see cref="Stream"/> is owned by the <see cref="IFileStreamProvider"/> and is short lived. It should be buffered if it needs use outside the lifetime of the <see cref="IFileStreamProvider"/>.</remarks>
		Task<Stream> GetResult(CancellationToken cancellationToken);

		/// <summary>
		/// Sets an <paramref name="errorCode"/> that indicates why the consuming operation could not complete. May only be called once on a <see cref="IFileStreamProvider"/>.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorCode"/> to set.</param>
		/// <param name="additionalData">Any additional information that can be provided about the error.</param>
		void SetError(ErrorCode errorCode, string additionalData);
	}
}
