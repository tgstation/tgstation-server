using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	[Model(RightsType.Administration, WriteRight = AdministrationRights.EditUsers)]
	public sealed class User
	{
		/// <summary>
		/// The ID of the <see cref="User"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public long Id { get; set; }

		/// <summary>
		/// The name of the <see cref="User"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string Name { get; set; }

		/// <summary>
		/// The <see cref="Rights.AdministrationRights"/> for the <see cref="User"/>
		/// </summary>
		public AdministrationRights AdministrationRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstanceManagerRights"/> for the <see cref="User"/>
		/// </summary>
		public InstanceManagerRights InstanceManagerRights { get; set; }
	}
}