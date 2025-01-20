using System;

using Newtonsoft.Json;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// A response to a <see cref="Request.ServerUpdateRequest"/>.
	/// </summary>
	public sealed class ServerUpdateResponse : FileTicketResponse
	{
		/// <summary>
		/// The version of tgstation-server pending update.
		/// </summary>
		/// <example>6.12.3</example>
		public Version NewVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdateResponse"/> class.
		/// </summary>
		/// <param name="newVersion">The value of <see cref="NewVersion"/>.</param>
		/// <param name="fileTicket">The optional value of <see cref="FileTicketResponse.FileTicket"/>.</param>
		[JsonConstructor]
		public ServerUpdateResponse(Version newVersion, string? fileTicket)
		{
			NewVersion = newVersion ?? throw new ArgumentNullException(nameof(newVersion));
			FileTicket = fileTicket;
		}
	}
}
