using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class PermissionSet : Api.Models.PermissionSet
	{
		/// <summary>
		/// <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="User"/>.
		/// </summary>
		public long? UserId { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Group"/>.
		/// </summary>
		public long? GroupId { get; set; }

		/// <summary>
		/// The <see cref="Models.User"/> the <see cref="PermissionSet"/> belongs to, if it is for a <see cref="Models.User"/>.
		/// </summary>
		public User? User { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> the <see cref="PermissionSet"/> belongs to, if it is for a <see cref="UserGroup"/>.
		/// </summary>
		public UserGroup? Group { get; set; }

		/// <summary>
		/// The <see cref="InstancePermissionSet"/>s associated with the <see cref="PermissionSet"/>.
		/// </summary>
		[BackingField(nameof(instancePermissionSets))]
		public ICollection<InstancePermissionSet> InstancePermissionSets
		{
			get => instancePermissionSets ?? throw new InvalidOperationException("InstancePermissionSets not set!");
			set => instancePermissionSets = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.PermissionSet.AdministrationRights"/>.
		/// </summary>
		[NotMapped]
		public new AdministrationRights AdministrationRights
		{
			get => base.AdministrationRights ?? throw new InvalidOperationException("AdministrationRights was null!");
			set => base.AdministrationRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.PermissionSet.InstanceManagerRights"/>.
		/// </summary>
		[NotMapped]
		public new InstanceManagerRights InstanceManagerRights
		{
			get => base.InstanceManagerRights ?? throw new InvalidOperationException("InstanceManagerRights was null!");
			set => base.InstanceManagerRights = value;
		}

		/// <summary>
		/// Backing field for <see cref="InstancePermissionSets"/>.
		/// </summary>
		ICollection<InstancePermissionSet>? instancePermissionSets;

		/// <summary>
		/// Convert the <see cref="PermissionSet"/> to it's API form.
		/// </summary>
		/// <returns>A new <see cref="Api.Models.PermissionSet"/>.</returns>
		public Api.Models.PermissionSet ToApi() => new ()
		{
			Id = Id,
			AdministrationRights = AdministrationRights,
			InstanceManagerRights = InstanceManagerRights,
		};
	}
}
