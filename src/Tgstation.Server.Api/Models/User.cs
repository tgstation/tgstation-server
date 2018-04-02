using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	public sealed class User
	{
		/// <summary>
		/// The system identifier for the <see cref="User"/>. On Windows, this is the user's SecurityIdentifier. On UNIX, this is their UID
		/// </summary>
		public string SystemIdentifier { get; set; }

		/// <summary>
		/// The name of the <see cref="User"/>
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The <see cref="Rights.AdministrationRights"/> for the <see cref="User"/>
		/// </summary>
		public AdministrationRights AdministrationRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstanceManagerRights"/> for the <see cref="User"/>
		/// </summary>
		public InstanceManagerRights InstanceManagerRights { get; set; }

		/// <summary>
		/// <see cref="List{T}"/> of <see cref="Instance.Id"/>s the <see cref="User"/> can view
		/// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
		public List<long> ViewableInstanceIds { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
	}
}