using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Service for temporarily storing files to be downloaded or uploaded.
	/// </summary>
	public interface IFileTransferTicketProvider
	{
		/// <summary>
		/// Create a <see cref="FileTicketResponse"/> for a download.
		/// </summary>
		/// <param name="fileDownloadProvider">The <see cref="FileDownloadProvider"/>.</param>
		/// <returns>A new <see cref="FileTicketResponse"/> for a download.</returns>
		FileTicketResponse CreateDownload(FileDownloadProvider fileDownloadProvider);

		/// <summary>
		/// Create a <see cref="IFileUploadTicket"/>.
		/// </summary>
		/// <param name="requiresSynchronousIO">If synchronous IO is required on the provided stream.</param>
		/// <returns>A new <see cref="IFileUploadTicket"/>.</returns>
		IFileUploadTicket CreateUpload(bool requiresSynchronousIO);
	}
}
