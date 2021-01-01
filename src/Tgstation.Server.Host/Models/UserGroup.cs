using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class UserGroup : Api.Models.Internal.UserGroupBase, IApiTransformable<Api.Models.UserGroup>
	{
		/// <summary>
		/// The <see cref="Models.PermissionSet"/> the <see cref="UserGroup"/> has.
		/// </summary>
		[Required]
		public PermissionSet PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="User"/>s the <see cref="UserGroup"/> has.
		/// </summary>
		public ICollection<User> Users { get; set; }

		/// <summary>
		/// Convert the <see cref="UserGroup"/> to it's API form.
		/// </summary>
		/// <param name="showUsers">If <see cref="Api.Models.UserGroup.Users"/> should be populated.</param>
		/// <returns>A new <see cref="Api.Models.UserGroup"/>.</returns>
		public Api.Models.UserGroup ToApi(bool showUsers) => new Api.Models.UserGroup
		{
			Id = Id,
			Name = Name,
			PermissionSet = PermissionSet.ToApi(),
			Users = showUsers
				? Users
					?.Select(x => x.ToApi(false))
					.OfType<Api.Models.Internal.UserBase>()
					.ToList()
					?? new List<Api.Models.Internal.UserBase>()
				: null,
		};

		/// <inheritdoc />
		public Api.Models.UserGroup ToApi() => ToApi(true);
	}
}
