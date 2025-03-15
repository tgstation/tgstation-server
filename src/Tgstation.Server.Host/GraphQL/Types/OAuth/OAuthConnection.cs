using System;

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
		public OAuthProvider Provider { get; }

		/// <summary>
		/// The ID of the user in the <see cref="Provider"/>.
		/// </summary>
		public string ExternalUserId { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConnection"/> class.
		/// </summary>
		/// <param name="externalUserId">The value of <see cref="ExternalUserId"/>.</param>
		/// <param name="provider">The value of <see cref="OAuthProvider"/>.</param>
		public OAuthConnection(string externalUserId, OAuthProvider provider)
		{
			ExternalUserId = externalUserId ?? throw new ArgumentNullException(nameof(externalUserId));
			Provider = provider;
		}
	}
}
