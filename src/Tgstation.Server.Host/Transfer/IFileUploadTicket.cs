using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.IO;

#nullable disable

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
		void SetError(ErrorCode errorCode, string additionalData);
	}
}
