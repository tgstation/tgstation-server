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
		public required AdministrationRights AdministrationRights { get; init; }

		/// <summary>
		/// The <see cref="Api.Rights.InstanceManagerRights"/> for the <see cref="PermissionSet"/>.
		/// </summary>
		public required InstanceManagerRights InstanceManagerRights { get; init; }
	}
}
