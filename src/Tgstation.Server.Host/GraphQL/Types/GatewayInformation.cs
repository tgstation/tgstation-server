using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Authorization;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Types.OAuth;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Security.RightsEvaluation;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents information about a <see cref="SwarmNode"/> retrieved via a <see cref="Interfaces.IGateway"/>.
	/// </summary>
	public sealed class GatewayInformation
	{
		/// <summary>
		/// Access the GraphQL API <see cref="global::System.Version"/> without auth.
		/// </summary>
		static Version GraphQLApiVersionNoAuth { get; } = global::System.Version.Parse(MasterVersionsAttribute.Instance.RawGraphQLVersion);

		/// <summary>
		/// Gets the major GraphQL API <see cref="global::System.Version"/> number of the <see cref="SwarmNode"/>.
		/// </summary>
		public int MajorGraphQLApiVersion => GraphQLApiVersionNoAuth.Major;

		/// <summary>
		/// Gets the minimum valid password length for TGS users.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the minimumn valid password length for TGS users.</returns>
		[Authorize]
		public async ValueTask<uint> MinimumPasswordLength(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			await authorizationService.CheckGraphQLAuthorized(
				[new OrRightsConditional<AdministrationRights>(
					new FlagRightsConditional<AdministrationRights>(AdministrationRights.WriteUsers),
					new FlagRightsConditional<AdministrationRights>(AdministrationRights.EditOwnPassword))],
				null);

			return generalConfigurationOptions.Value.MinimumPasswordLength;
		}

		/// <summary>
		/// Gets the maximum allowed attached instances for the <see cref="SwarmNode"/>.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed attached instances for the <see cref="SwarmNode"/>.</returns>
		[Authorize]
		public async ValueTask<uint> InstanceLimit(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			await authorizationService.CheckGraphQLAuthorized(
				[new FlagRightsConditional<InstanceManagerRights>(InstanceManagerRights.Create)],
				null);

			return generalConfigurationOptions.Value.InstanceLimit;
		}

		/// <summary>
		/// Gets the maximum allowed registered <see cref="User"/>s for the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed registered users for the <see cref="ServerSwarm"/>.</returns>
		/// <remarks>This limit only applies to user creation attempts made via the current <see cref="SwarmNode"/>.</remarks>
		[Authorize]
		public async ValueTask<uint> UserLimit(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			await authorizationService.CheckGraphQLAuthorized(
				[new FlagRightsConditional<AdministrationRights>(AdministrationRights.WriteUsers)],
				null);

			return generalConfigurationOptions.Value.UserLimit;
		}

		/// <summary>
		/// Gets the maximum allowed registered <see cref="UserGroup"/>s for the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed registered <see cref="UserGroup"/>s for the <see cref="ServerSwarm"/>.</returns>
		/// <remarks>This limit only applies to <see cref="UserGroup"/> creation attempts made via the current <see cref="SwarmNode"/>.</remarks>
		[Authorize]
		public async ValueTask<uint> UserGroupLimit(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			await authorizationService.CheckGraphQLAuthorized(
				[new FlagRightsConditional<AdministrationRights>(AdministrationRights.WriteUsers)],
				null);
			return generalConfigurationOptions.Value.UserGroupLimit;
		}

		/// <summary>
		/// Gets the locations <see cref="Instance"/>s may be created or attached from if there are restrictions.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>The locations <see cref="Instance"/>s may be created or attached from if there are restrictions, <see langword="null"/> otherwise.</returns>
		[Authorize]
		public async ValueTask<IReadOnlyCollection<string>?> ValidInstancePaths(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			await authorizationService.CheckGraphQLAuthorized(
				[new OrRightsConditional<InstanceManagerRights>(
					new FlagRightsConditional<InstanceManagerRights>(InstanceManagerRights.Create),
					new FlagRightsConditional<InstanceManagerRights>(InstanceManagerRights.Relocate))],
				null);
			return generalConfigurationOptions.Value.ValidInstancePaths;
		}

		/// <summary>
		/// Gets a flag indicating whether or not the current <see cref="SwarmNode"/> runs on a Windows operating system.
		/// </summary>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <returns><see langword="true"/> if the <see cref="SwarmNode"/> runs on a Windows operating system, <see langword="false"/> otherwise.</returns>
		[Authorize]
		public bool WindowsHost(
			[Service] IPlatformIdentifier platformIdentifier)
		{
			ArgumentNullException.ThrowIfNull(platformIdentifier);

			return platformIdentifier.IsWindows;
		}

		/// <summary>
		/// Gets the swarm protocol <see cref="Version"/>.
		/// </summary>
		/// <returns>The swarm protocol <see cref="global::System.Version"/>.</returns>
		[Authorize]
		public Version SwarmProtocolVersion()
			=> global::System.Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion);

		/// <summary>
		/// Gets the <see cref="global::System.Version"/> of tgstation-server the <see cref="SwarmNode"/> is running.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <returns>The <see cref="global::System.Version"/> of tgstation-server the <see cref="SwarmNode"/> is running.</returns>
		[Authorize]
		public Version Version(
			[Service] IAssemblyInformationProvider assemblyInformationProvider)
		{
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			return assemblyInformationProvider.Version;
		}

		/// <summary>
		/// Gets the GraphQL API <see cref="global::System.Version"/> of the <see cref="SwarmNode"/>.
		/// </summary>
		/// <returns>The GraphQL API <see cref="global::System.Version"/> of the <see cref="SwarmNode"/>.</returns>
		[Authorize]
		public Version GraphQLApiVersion()
			=> GraphQLApiVersionNoAuth;

		/// <summary>
		/// Gets the REST API <see cref="global::System.Version"/> of the <see cref="SwarmNode"/>.
		/// </summary>
		/// <returns>The REST API <see cref="global::System.Version"/> of the <see cref="SwarmNode"/>.</returns>
		[Authorize]
		public Version ApiVersion() => ApiHeaders.Version;

		/// <summary>
		/// Gets the DMAPI interop <see cref="global::System.Version"/> the <see cref="SwarmNode"/> uses.
		/// </summary>
		/// <returns>Yhe DMAPI interop <see cref="global::System.Version"/> the <see cref="SwarmNode"/> uses.</returns>
		[Authorize]
		public Version DMApiVersion()
			=> DMApiConstants.InteropVersion;

		/// <summary>
		/// Gets the information needed to perform open authentication with the <see cref="SwarmNode"/>.
		/// </summary>
		/// <param name="oAuthProviders">The <see cref="IOAuthProviders"/> to use.</param>
		/// <returns>A map of enabled <see cref="OAuthProvider"/>s to their <see cref="FullOAuthProviderInfo"/>.</returns>
		public OAuthProviderInfos OAuthProviderInfos(
			[Service] IOAuthProviders oAuthProviders)
		{
			ArgumentNullException.ThrowIfNull(oAuthProviders);
			return new OAuthProviderInfos(oAuthProviders);
		}
	}
}
