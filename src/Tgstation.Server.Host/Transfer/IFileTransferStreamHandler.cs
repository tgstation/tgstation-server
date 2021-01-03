using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Reads and writes to <see cref="Stream"/>s associated with <see cref="FileTicketResult"/>s.
	/// </summary>
	public interface IFileTransferStreamHandler
	{
		/// <summary>
		/// Sets the <see cref="Stream"/> for a given <paramref name="ticket"/> associated with a pending upload.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResult"/>.</param>
		/// <param name="stream">The <see cref="Stream"/> with uploaded data.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns><see langword="null"/> if the upload completed successfully, <see cref="ErrorMessage"/> otherwise.</returns>
		Task<ErrorMessage> SetUploadStream(FileTicketResult ticket, Stream stream, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the the <see cref="Stream"/> for a given <paramref name="ticket"/> associated with a pending download.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResult"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Tuple{T1, T2}"/> containing either a <see cref="Stream"/> containing the data to download or an <see cref="ErrorMessage"/> to return.</returns>
		Task<Tuple<FileStream, ErrorMessage>> RetrieveDownloadStream(FileTicketResult ticket, CancellationToken cancellationToken);
	}
}
