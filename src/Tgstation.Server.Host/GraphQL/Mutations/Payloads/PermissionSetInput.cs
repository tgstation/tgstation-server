using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.GraphQL.Mutations.Payloads
{
	/// <summary>
	/// Updates a set of permissions for the server. <see langword="null"/> values default to their "None" variants.
	/// </summary>
	public sealed class PermissionSetInput
	{
		/// <summary>
		/// The <see cref="Api.Rights.AdministrationRights"/> for the <see cref="Types.PermissionSet"/>.
		/// </summary>
		public required AdministrationRights? AdministrationRights { get; init; }

		/// <summary>
		/// The <see cref="Api.Rights.InstanceManagerRights"/> for the <see cref="Types.PermissionSet"/>.
		/// </summary>
		public required InstanceManagerRights? InstanceManagerRights { get; init; }
	}
}
