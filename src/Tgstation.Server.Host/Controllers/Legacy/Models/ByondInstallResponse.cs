using System;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Controllers.Legacy.Models
{
	/// <summary>
	/// Represents a BYOND installation job. <see cref="FileTicketResponse.FileTicket"/> is used to upload custom BYOND version zip files.
	/// </summary>
	public sealed class ByondInstallResponse : FileTicketResponse
	{
		/// <summary>
		/// The <see cref="JobResponse"/> being used to install a new <see cref="Version"/>.
		/// </summary>
		[ResponseOptions]
		public JobResponse InstallJob { get; set; }

		/// <inheritdoc />
		[ResponseOptions]
		public override string FileTicket
		{
			get => base.FileTicket;
			set => base.FileTicket = value;
		}
	}
}
