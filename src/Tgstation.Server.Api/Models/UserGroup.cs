using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class UserGroup : Internal.UserGroup
	{
		/// <summary>
		/// The <see cref="Models.PermissionSet"/> of the <see cref="UserGroup"/>.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="User"/>s the <see cref="UserGroup"/> has.
		/// </summary>
		public ICollection<Internal.User>? Users { get; set; }
	}
}
