using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class PermissionSet : Api.Models.PermissionSet
	{
		/// <summary>
		/// The <see cref="Api.Models.Internal.User.Id"/> of <see cref="User"/>.
		/// </summary>
		public long? UserId { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Group"/>.
		/// </summary>
		public long? GroupId { get; set; }

		/// <summary>
		/// The <see cref="Models.User"/> the <see cref="PermissionSet"/> belongs to, if it is for a <see cref="Models.User"/>.
		/// </summary>
		public User User { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> the <see cref="PermissionSet"/> belongs to, if it is for a <see cref="UserGroup"/>.
		/// </summary>
		public UserGroup Group { get; set; }

		/// <summary>
		/// The <see cref="InstancePermissionSet"/>s associated with the <see cref="PermissionSet"/>.
		/// </summary>
		public ICollection<InstancePermissionSet> InstancePermissionSets { get; set; }

		/// <summary>
		/// Convert the <see cref="PermissionSet"/> to it's API form.
		/// </summary>
		/// <returns>A new <see cref="Api.Models.PermissionSet"/>.</returns>
		public Api.Models.PermissionSet ToApi() => new Api.Models.PermissionSet
		{
			Id = Id,
			AdministrationRights = AdministrationRights,
			InstanceManagerRights = InstanceManagerRights,
		};
	}
}
