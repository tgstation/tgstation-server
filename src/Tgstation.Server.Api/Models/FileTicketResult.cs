namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Response for when file transfers are necessary.
	/// </summary>
	public class FileTicketResult
	{
		/// <summary>
		/// The ticket to use to access the <see cref="Routes.Transfer"/> controller.
		/// </summary>
		public string? FileTicket { get; set; }
	}
}
