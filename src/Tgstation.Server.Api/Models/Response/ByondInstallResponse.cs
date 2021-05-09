using System;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents a BYOND installation. <see cref="FileTicketResponse.FileTicket"/> is used to upload custom BYOND version zip files, though <see cref="Version"/> must still be set.
	/// </summary>
	public sealed class ByondInstallResponse : FileTicketResponse
	{
		/// <summary>
		/// The <see cref="JobResponse"/> being used to install a new <see cref="Version"/>.
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
