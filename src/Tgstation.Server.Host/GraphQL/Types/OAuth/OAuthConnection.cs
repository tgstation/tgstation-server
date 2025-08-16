using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// Represents a valid OAuth connection.
	/// </summary>
	public sealed class OAuthConnection
	{
		/// <summary>
		/// The <see cref="OAuthProvider"/> of the <see cref="OAuthConnection"/>.
		/// </summary>
		public required OAuthProvider Provider { get; init;  }

		/// <summary>
		/// The ID of the user in the <see cref="Provider"/>.
		/// </summary>
		public required string ExternalUserId { get; init; }
	}
}
