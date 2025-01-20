using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents administrative server information.
	/// </summary>
	public sealed class AdministrationResponse : UpdateInformation
	{
		/// <summary>
		/// The GitHub repository the server is built to receive updates from.
		/// </summary>
		/// <example>https://github.com/tgstation/tgstation</example>
		public Uri? TrackedRepositoryUrl { get; set; }
	}
}
