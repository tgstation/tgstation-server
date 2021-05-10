using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// A <see cref="FileTicketResponse"/> that waits for a pending upload.
	/// </summary>
	public interface IFileUploadTicket : IDisposable
	{
		/// <summary>
		/// The <see cref="FileTicketResponse"/>.
		/// </summary>
		FileTicketResponse Ticket { get; }

		/// <summary>
		/// Gets the <see cref="Stream"/> for the uploaded file.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the uploaded <see cref="Stream"/> of the file on success, <see langword="null"/> if the ticket timed out.</returns>
		/// <remarks>The resulting <see cref="Stream"/> is short lived and should be buffered if it needs use outside the lifetime of the <see cref="IFileUploadTicket"/>.</remarks>
		Task<Stream> GetResult(CancellationToken cancellationToken);

		/// <summary>
		/// Sets an <paramref name="errorMessage"/> for the upload. Will be returned in upload request as a 409 error.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> to set.</param>
		void SetErrorMessage(ErrorMessageResponse errorMessage);
	}
}
