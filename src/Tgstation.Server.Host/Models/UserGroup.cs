using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class UserGroup : NamedEntity, IApiTransformable<UserGroupResponse>
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
		/// <param name="showUsers">If <see cref="UserGroupResponse.Users"/> should be populated.</param>
		/// <returns>A new <see cref="UserGroupResponse"/>.</returns>
		public UserGroupResponse ToApi(bool showUsers) => new UserGroupResponse
		{
			Id = Id,
			Name = Name,
			PermissionSet = PermissionSet.ToApi(),
			Users = showUsers
				? Users
					?.Select(x => x.CreateUserName())
					.ToList()
					?? new List<UserName>()
				: null,
		};

		/// <inheritdoc />
		public UserGroupResponse ToApi() => ToApi(true);
	}
}
