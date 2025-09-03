using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a set of permissions for the server.
	/// </summary>
	public sealed class PermissionSet
	{
		/// <summary>
		/// The <see cref="Api.Rights.AdministrationRights"/> for the <see cref="PermissionSet"/>.
		/// </summary>
		public required RightsHolder<AdministrationRights> AdministrationRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.InstanceManagerRights"/> for the <see cref="PermissionSet"/>.
		/// </summary>
		public required RightsHolder<InstanceManagerRights> InstanceManagerRights { get; set; }
	}
}
