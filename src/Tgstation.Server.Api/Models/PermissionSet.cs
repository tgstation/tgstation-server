using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a set of server permissions.
	/// </summary>
	public class PermissionSet
	{
		/// <summary>
		/// The ID of the <see cref="PermissionSet"/>.
		/// </summary>
		[Required]
		public long? Id { get; set; }

		/// <summary>
		/// The <see cref="Rights.AdministrationRights"/> for the <see cref="User"/>
		/// </summary>
		[Required]
		public AdministrationRights? AdministrationRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstanceManagerRights"/> for the <see cref="User"/>
		/// </summary>
		[Required]
		public InstanceManagerRights? InstanceManagerRights { get; set; }
	}
}
