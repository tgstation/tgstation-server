using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Service for temporarily storing files to be downloaded or uploaded.
	/// </summary>
	public interface IFileTransferTicketProvider
	{
		/// <summary>
		/// Create a <see cref="FileTicketResult"/> for a download.
		/// </summary>
		/// <param name="fileDownloadProvider">The <see cref="FileDownloadProvider"/>.</param>
		/// <returns>A new <see cref="FileTicketResult"/> for a download.</returns>
		FileTicketResult CreateDownload(FileDownloadProvider fileDownloadProvider);

		/// <summary>
		/// Create a <see cref="IFileUploadTicket"/>.
		/// </summary>
		/// <returns>A new <see cref="IFileUploadTicket"/>.</returns>
		IFileUploadTicket CreateUpload();
	}
}
