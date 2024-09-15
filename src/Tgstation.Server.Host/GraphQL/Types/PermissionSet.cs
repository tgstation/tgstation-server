using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types.Relay;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a set of permissions for the server.
	/// </summary>
	[Node]
	public sealed class PermissionSet : Entity
	{
		/// <summary>
		/// Node resolver for <see cref="PermissionSet"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IPermissionSetAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="PermissionSet"/>, if present.</returns>
		[TgsGraphQLAuthorize]
		public static ValueTask<PermissionSet?> GetPermissionSet(
			long id,
			[Service] IGraphQLAuthorityInvoker<IPermissionSetAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.PermissionSet, PermissionSet, PermissionSetGraphQLTransformer>(
				authority => authority.GetId(id, PermissionSetLookupType.Id, cancellationToken));
		}

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
