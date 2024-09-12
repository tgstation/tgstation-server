using System.Diagnostics.CodeAnalysis;

using HotChocolate.Types.Relay;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a set of permissions for the server.
	/// </summary>
	[Node]
	public sealed class PermissionSet : Entity
	{
		/// <summary>
		/// The <see cref="Api.Rights.AdministrationRights"/> for the <see cref="PermissionSet"/>.
		/// </summary>
		public AdministrationRights AdministrationRights { get; }

		/// <summary>
		/// The <see cref="Api.Rights.InstanceManagerRights"/> for the <see cref="PermissionSet"/>.
		/// </summary>
		public InstanceManagerRights InstanceManagerRights { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionSet"/> class.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/>.</param>
		/// <param name="administrationRights">The value of <see cref="AdministrationRights"/>.</param>
		/// <param name="instanceManagerRights">The value of <see cref="InstanceManagerRights"/>.</param>
		[SetsRequiredMembers]
		public PermissionSet(long id, AdministrationRights administrationRights, InstanceManagerRights instanceManagerRights)
			: base(id)
		{
			AdministrationRights = administrationRights;
			InstanceManagerRights = instanceManagerRights;
		}
	}
}
