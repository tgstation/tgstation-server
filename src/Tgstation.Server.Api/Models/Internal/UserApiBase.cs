using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <inheritdoc />
	public abstract class UserApiBase : UserModelBase
	{
		/// <summary>
		/// List of <see cref="OAuthConnection"/>s associated with the user.
		/// </summary>
		public ICollection<OAuthConnection>? OAuthConnections { get; set; }

		/// <summary>
		/// List of <see cref="OidcConnection"/>s associated with the user.
		/// </summary>
		public ICollection<OidcConnection>? OidcConnections { get; set; }

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> directly associated with the user.
		/// </summary>
		[ResponseOptions]
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> asociated with the user, if any.
		/// </summary>
		[ResponseOptions]
		public UserGroup? Group { get; set; }
	}
}
