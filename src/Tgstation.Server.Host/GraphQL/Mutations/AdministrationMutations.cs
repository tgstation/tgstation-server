using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Scalars;

namespace Tgstation.Server.Host.GraphQL.Mutations
{
	/// <summary>
	/// <see cref="IAdministrationAuthority"/> related <see cref="Mutation"/>s.
	/// </summary>
	[ExtendObjectType(typeof(Mutation))]
	[GraphQLDescription(Mutation.GraphQLDescription)]
	public sealed class AdministrationMutations
	{
		/// <summary>
		/// Restarts the mutated <see cref="Interfaces.IServerNode"/> without terminating running game instances.
		/// </summary>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/>.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		[Error(typeof(ErrorMessageException))]
		public async ValueTask<Query> RestartServerNode(
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority)
		{
			ArgumentNullException.ThrowIfNull(administrationAuthority);
			await administrationAuthority.Invoke(
				authority => authority.TriggerServerRestart());

			return new Query();
		}

		/// <summary>
		/// Restarts the mutated <see cref="Interfaces.IServerNode"/> without terminating running game instances and changes its <paramref name="targetVersion"/>.
		/// </summary>
		/// <param name="targetVersion">The semver of the server <see cref="Version"/> available in the tracked repository to switch to.</param>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		[Error(typeof(ErrorMessageException))]
		public async ValueTask<Query> ChangeServerNodeVersionViaTrackedRepository(
			Version targetVersion,
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(targetVersion);
			ArgumentNullException.ThrowIfNull(administrationAuthority);
			await administrationAuthority.Invoke<ServerUpdateResponse, ServerUpdateResponse>(
				authority => authority.TriggerServerVersionChange(targetVersion, false, cancellationToken));
			return new Query();
		}

		/// <summary>
		/// Restarts the mutated <see cref="Interfaces.IServerNode"/> without terminating running game instances and changes its <paramref name="targetVersion"/>.
		/// </summary>
		/// <param name="targetVersion">The semver of the server <see cref="Version"/> available in the tracked repository to switch to.</param>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A FileTicket that should be used to upload a zip containing the update data to the file transfer service.</returns>
		[Error(typeof(ErrorMessageException))]
		[GraphQLType<FileUploadTicketType>]
		public async ValueTask<string> ChangeServerNodeVersionViaUpload(
			Version targetVersion,
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(targetVersion);
			ArgumentNullException.ThrowIfNull(administrationAuthority);
			var response = await administrationAuthority.Invoke<ServerUpdateResponse, FileTicketResponse>(
				authority => authority.TriggerServerVersionChange(targetVersion, true, cancellationToken));

			return response.FileTicket ?? throw new InvalidOperationException("Administration authority did not generate a FileUploadTicket!");
		}
	}
}
