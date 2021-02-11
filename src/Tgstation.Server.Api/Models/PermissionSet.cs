using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a set of server permissions.
	/// </summary>
	public class PermissionSet : EntityId
	{
		/// <inheritdoc />
		[RequestOptions(FieldPresence.Ignored)]
		public override long? Id
		{
			get => base.Id;
			set => base.Id = value;
		}

		/// <summary>
		/// The <see cref="Rights.AdministrationRights"/> for the user.
		/// </summary>
		[Required]
		public AdministrationRights? AdministrationRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstanceManagerRights"/> for the user.
		/// </summary>
		[Required]
		public InstanceManagerRights? InstanceManagerRights { get; set; }
	}
}
