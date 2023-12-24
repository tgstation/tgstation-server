using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// A <see cref="FileTicketResponse"/> that waits for a pending upload.
	/// </summary>
	public interface IFileUploadTicket : IFileStreamProvider
	{
		/// <summary>
		/// The <see cref="FileTicketResponse"/>.
		/// </summary>
		FileTicketResponse Ticket { get; }

		/// <summary>
		/// Sets an <paramref name="errorCode"/> that indicates why the consuming operation could not complete. May only be called once on a <see cref="IFileStreamProvider"/>.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorCode"/> to set.</param>
		/// <param name="additionalData">Any additional information that can be provided about the error.</param>
		void SetError(ErrorCode errorCode, string? additionalData);

		/// <summary>
		/// Gets the provided <see cref="Stream"/>. May be called multiple times, though cancelling any may cause all calls to be cancelled. All calls yield the same <see cref="Stream"/> reference.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the provided <see cref="Stream"/> on success, <see langword="null"/> if the upload expired.</returns>
		/// <remarks>The resulting <see cref="Stream"/> is owned by the <see cref="IFileStreamProvider"/> and is short lived unless otherwise specified. It should be buffered if it needs use outside the lifetime of the <see cref="IFileStreamProvider"/>.</remarks>
		new ValueTask<Stream?> GetResult(CancellationToken cancellationToken);
	}
}
