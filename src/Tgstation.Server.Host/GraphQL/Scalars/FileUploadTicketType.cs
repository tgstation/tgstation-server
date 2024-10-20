namespace Tgstation.Server.Host.GraphQL.Scalars
{
	/// <summary>
	/// A <see cref="StringScalarType"/> for upload <see cref="Api.Models.Response.FileTicketResponse"/>s.
	/// </summary>
	public sealed class FileUploadTicketType : StringScalarType
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FileUploadTicketType"/> class.
		/// </summary>
		public FileUploadTicketType()
			: base("FileUploadTicket")
		{
			Description = "Represents a ticket that can be used with the file transfer service to upload a file";
		}
	}
}
