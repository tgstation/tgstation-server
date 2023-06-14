namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Determines the type of <see cref="global::System.IO.Stream"/> returned from <see cref="IFileUploadTicket"/>'s created from <see cref="IFileTransferTicketProvider"/>s.
	/// </summary>
	public enum FileUploadStreamKind
	{
		/// <summary>
		/// Stream is the unbuffered request.
		/// </summary>
		None,

		/// <summary>
		/// Use a <see cref="Microsoft.AspNetCore.WebUtilities.FileBufferingReadStream"/> as a backend.
		/// </summary>
		ForSynchronousIO,
	}
}
