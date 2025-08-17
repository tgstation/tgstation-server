using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a group of <see cref="User"/>s.
	/// </summary>
	public sealed class UserGroup : NamedEntity, ILegacyApiTransformable<UserGroupResponse>, IApiTransformable<UserGroup, GraphQL.Types.UserGroup>
	{
		/// <summary>
		/// The <see cref="Models.PermissionSet"/> the <see cref="UserGroup"/> has.
		/// </summary>
		[Required]
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="User"/>s the <see cref="UserGroup"/> has.
		/// </summary>
		public ICollection<User>? Users { get; set; }

		/// <summary>
		/// Convert the <see cref="UserGroup"/> to it's API form.
		/// </summary>
		/// <param name="showUsers">If <see cref="UserGroupResponse.Users"/> should be populated.</param>
		/// <returns>A new <see cref="UserGroupResponse"/>.</returns>
		public UserGroupResponse ToApi(bool showUsers) => new()
		{
			Id = Id,
			Name = Name,
			PermissionSet = (PermissionSet ?? throw new InvalidOperationException("PermissionSet must be set!")).ToApi(),
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
