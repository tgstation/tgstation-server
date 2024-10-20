using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Gets information about updates for the <see cref="ServerSwarm"/>.
	/// </summary>
	public sealed class UpdateInformation : IDisposable
	{
		/// <summary>
		/// <see cref="SemaphoreSlim"/> to prevent duplicate cache generations in one query.
		/// </summary>
		readonly SemaphoreSlim cacheReadSemaphore;

		/// <summary>
		/// If the cache was already force generated this query.
		/// </summary>
		bool cacheForceGenerated;

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdateInformation"/> class.
		/// </summary>
		public UpdateInformation()
		{
			cacheReadSemaphore = new SemaphoreSlim(1, 1);
		}

		/// <inheritdoc />
		public void Dispose()
			=> cacheReadSemaphore.Dispose();

		/// <summary>
		/// If there is a swarm update in progress. This is not affected by <see cref="GeneratedAt(IGraphQLAuthorityInvoker{IAdministrationAuthority}, CancellationToken)"/>.
		/// </summary>
		/// <param name="serverControl">The <see cref="IServerControl"/> to use.</param>
		/// <returns><see langword="true"/> if there is an update in progress, <see langword="false"/> otherwise.</returns>
		public bool UpdateInProgress(
			[Service] IServerControl serverControl)
		{
			ArgumentNullException.ThrowIfNull(serverControl);
			return serverControl.UpdateInProgress;
		}

		/// <summary>
		/// Gets the <see cref="Uri"/> of the GitHub repository updates are sourced from.
		/// </summary>
		/// <param name="forceFresh">If <see langword="true"/> the local cache TGS keeps of this data will be bypassed.</param>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="Uri"/> of the GitHub repository updates are sourced from on success. <see langword="null"/> if a GitHub API error occurred.</returns>
		public async ValueTask<Uri?> TrackedRepositoryUrl(
			bool forceFresh,
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
			=> (await GetAdministrationResponseSafe(forceFresh, administrationAuthority, cancellationToken)).TrackedRepositoryUrl;

		/// <summary>
		/// Gets the time the <see cref="UpdateInformation"/> was generated.
		/// </summary>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The time the <see cref="UpdateInformation"/> was generated on success. <see langword="null"/> if a GitHub API error occurred.</returns>
		public async ValueTask<DateTimeOffset?> GeneratedAt(
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
			=> (await GetAdministrationResponseSafe(false, administrationAuthority, cancellationToken)).GeneratedAt;

		/// <summary>
		/// Gets the latest <see cref="Version"/> of tgstation-server available on the GitHub repository updates are sourced from.
		/// </summary>
		/// <param name="forceFresh">If <see langword="true"/> the local cache TGS keeps of this data will be bypassed.</param>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="Version"/> of the latest TGS version on success. <see langword="null"/> if a GitHub API error occurred.</returns>
		public async ValueTask<Version?> LatestVersion(
			bool forceFresh,
			[Service] IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
			=> (await GetAdministrationResponseSafe(forceFresh, administrationAuthority, cancellationToken)).LatestVersion;

		/// <summary>
		/// Safely retrieve the <see cref="AdministrationResponse"/> from a given <paramref name="administrationAuthority"/> without generating the cache multiple times in one query.
		/// </summary>
		/// <param name="forceFresh">If <see langword="true"/> the local cache TGS keeps of this data will be bypassed.</param>
		/// <param name="administrationAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AdministrationResponse"/> from the <paramref name="administrationAuthority"/>.</returns>
		async ValueTask<AdministrationResponse> GetAdministrationResponseSafe(
			bool forceFresh,
			IGraphQLAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(cacheReadSemaphore, cancellationToken))
			{
				if (cacheForceGenerated)
					forceFresh = false;
				else
					cacheForceGenerated |= forceFresh;

				ArgumentNullException.ThrowIfNull(administrationAuthority);
				var response = await administrationAuthority.Invoke<AdministrationResponse, AdministrationResponse>(
					authority => authority.GetUpdateInformation(forceFresh, cancellationToken));

				return response;
			}
		}
	}
}
