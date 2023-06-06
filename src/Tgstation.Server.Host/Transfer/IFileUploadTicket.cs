using Tgstation.Server.Api.Models.Response;

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
	}
}
