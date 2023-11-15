namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an engine installation job. <see cref="FileTicketResponse.FileTicket"/> is used to upload custom version zip files.
	/// </summary>
	public sealed class EngineInstallResponse : FileTicketResponse
	{
		/// <summary>
		/// The <see cref="JobResponse"/> being used to install a new <see cref="Models.EngineVersion"/>.
		/// </summary>
		[ResponseOptions]
		public JobResponse? InstallJob { get; set; }

		/// <inheritdoc />
		[ResponseOptions]
		public override string? FileTicket
		{
			get => base.FileTicket;
			set => base.FileTicket = value;
		}
	}
}
