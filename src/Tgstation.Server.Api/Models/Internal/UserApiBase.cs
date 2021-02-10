using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <inheritdoc />
	public abstract class UserApiBase : UserModelBase
	{
		/// <summary>
		/// List of <see cref="OAuthConnection"/>s associated with the <see cref="UserResponse"/>.
		/// </summary>
		public ICollection<OAuthConnection>? OAuthConnections { get; set; }

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> directly associated with the <see cref="UserResponse"/>.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> asociated with the <see cref="UserResponse"/>, if any.
		/// </summary>
		[ResponseOptions]
		public UserGroup? Group { get; set; }
	}
}
