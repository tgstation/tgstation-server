using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Information about the local tgstation-server.
	/// </summary>
	public class LocalServerInformation : ServerInformationBase
	{
		/// <summary>
		/// If the server is running on a windows operating system.
		/// </summary>
		public bool WindowsHost { get; set; }

		/// <summary>
		/// Map of <see cref="OAuthProvider"/> to the <see cref="OAuthProviderInfo"/> for them.
		/// </summary>
		public Dictionary<OAuthProvider, OAuthProviderInfo>? OAuthProviderInfos { get; set; }
	}
}
