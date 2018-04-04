using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	[Model(typeof(AdministrationRights), WriteRight = AdministrationRights.EditUsers)]
	public sealed class User
	{
		/// <summary>
		/// The system identifier for the <see cref="User"/>. On Windows, this is the user's SecurityIdentifier. On UNIX, this is their UID
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string SystemIdentifier { get; set; }

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