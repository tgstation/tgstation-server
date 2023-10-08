using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IFileStreamProvider"/> that provides <see cref="MemoryStream"/>s.
	/// </summary>
	public interface ISeekableFileStreamProvider : IFileStreamProvider
	{
		/// <summary>
		/// If the <see cref="ISeekableFileStreamProvider"/> has had <see cref="global::System.IAsyncDisposable.DisposeAsync"/> called on it.
		/// </summary>
		bool Disposed { get; }

		/// <summary>
		/// Gets the provided <see cref="MemoryStream"/>. May be called multiple times, though cancelling any may cause all calls to be cancelled.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the provided <see cref="MemoryStream"/> on success, <see langword="null"/> if it could not be provided.</returns>
		ValueTask<MemoryStream> GetOwnedResult(CancellationToken cancellationToken);
	}
}
