namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Response for when file transfers are necessary.
	/// </summary>
	public class FileTicketResponse
	{
		/// <summary>
		/// The ticket to use to access the <see cref="Routes.Transfer"/> controller.
		/// </summary>
		public virtual string? FileTicket { get; set; }
	}
}
