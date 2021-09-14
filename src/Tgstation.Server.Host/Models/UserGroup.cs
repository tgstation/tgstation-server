using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class UserGroup : NamedEntity, IApiTransformable<UserGroupResponse>
	{
		/// <summary>
		/// <see cref="EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="NamedEntity.Name"/>.
		/// </summary>
		[NotMapped]
		public new string Name
		{
			get => base.Name ?? throw new InvalidOperationException("Name was null!");
			set => base.Name = value;
		}

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> the <see cref="UserGroup"/> has.
		/// </summary>
		[Required]
		[BackingField(nameof(permissionSet))]
		public PermissionSet PermissionSet
		{
			get => permissionSet ?? throw new InvalidOperationException("permissionSet not set!");
			set => permissionSet = value;
		}

		/// <summary>
		/// The <see cref="User"/>s the <see cref="UserGroup"/> has.
		/// </summary>
		[BackingField(nameof(users))]
		public ICollection<User> Users
		{
			get => users ?? throw new InvalidOperationException("users not set!");
			set => users = value;
		}

		/// <summary>
		/// Backing field for <see cref="PermissionSet"/>.
		/// </summary>
		PermissionSet? permissionSet;

		/// <summary>
		/// Backing field for <see cref="Users"/>.
		/// </summary>
		ICollection<User>? users;

		/// <summary>
		/// Convert the <see cref="UserGroup"/> to it's API form.
		/// </summary>
		/// <param name="showUsers">If <see cref="UserGroupResponse.Users"/> should be populated.</param>
		/// <returns>A new <see cref="UserGroupResponse"/>.</returns>
		public UserGroupResponse ToApi(bool showUsers) => new ()
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
