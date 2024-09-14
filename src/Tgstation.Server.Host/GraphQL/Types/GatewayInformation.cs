using System;
using System.Collections.Generic;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents information about a <see cref="SwarmNode"/> retrieved via a <see cref="Interfaces.IGateway"/>.
	/// </summary>
	public sealed class GatewayInformation
	{
		/// <summary>
		/// Gets the minimum valid password length for TGS users.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the minimumn valid password length for TGS users.</returns>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword)]
		public uint? MinimumPasswordLength(
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);
			return generalConfigurationOptions.Value.MinimumPasswordLength;
		}

		/// <summary>
		/// Gets the maximum allowed attached instances for the <see cref="SwarmNode"/>.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed attached instances for the <see cref="SwarmNode"/>.</returns>
		[TgsGraphQLAuthorize(InstanceManagerRights.Create)]
		public uint? InstanceLimit(
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);
			return generalConfigurationOptions.Value.InstanceLimit;
		}

		/// <summary>
		/// Gets the maximum allowed registered <see cref="User"/>s for the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed registered users for the <see cref="ServerSwarm"/>.</returns>
		/// <remarks>This limit only applies to user creation attempts made via the current <see cref="SwarmNode"/>.</remarks>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers)]
		public uint? UserLimit(
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);
			return generalConfigurationOptions.Value.UserLimit;
		}

		/// <summary>
		/// Gets the maximum allowed registered <see cref="UserGroup"/>s for the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>A <see cref="uint"/> specifying the maximum allowed registered <see cref="UserGroup"/>s for the <see cref="ServerSwarm"/>.</returns>
		/// <remarks>This limit only applies to <see cref="UserGroup"/> creation attempts made via the current <see cref="SwarmNode"/>.</remarks>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers)]
		public uint? UserGroupLimit(
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);
			return generalConfigurationOptions.Value.UserGroupLimit;
		}

		/// <summary>
		/// Gets the locations <see cref="Instance"/>s may be created or attached from if there are restrictions.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <returns>The locations <see cref="Instance"/>s may be created or attached from if there are restrictions, <see langword="null"/> otherwise.</returns>
		[TgsGraphQLAuthorize(InstanceManagerRights.Create | InstanceManagerRights.Relocate)]
		public IReadOnlyCollection<string>? ValidInstancePaths(
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);
			return generalConfigurationOptions.Value.ValidInstancePaths;
		}

		/// <summary>
		/// Gets a flag indicating whether or not the current <see cref="SwarmNode"/> runs on a Windows operating system.
		/// </summary>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <returns><see langword="true"/> if the <see cref="SwarmNode"/> runs on a Windows operating system, <see langword="false"/> otherwise.</returns>
		[TgsGraphQLAuthorize]
		public bool? WindowsHost(
			[Service] IPlatformIdentifier platformIdentifier)
		{
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			return platformIdentifier.IsWindows;
		}

		/// <summary>
		/// Gets the swarm protocol <see cref="Version"/>.
		/// </summary>
		[TgsGraphQLAuthorize]
		public Version? SwarmProtocolVersion => global::System.Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion);

		/// <summary>
		/// Gets the <see cref="global::System.Version"/> of tgstation-server the <see cref="SwarmNode"/> is running.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <returns>The <see cref="global::System.Version"/> of tgstation-server the <see cref="SwarmNode"/> is running.</returns>
		[TgsGraphQLAuthorize]
		public Version? Version(
			[Service] IAssemblyInformationProvider assemblyInformationProvider)
		{
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			return assemblyInformationProvider.Version;
		}

		/// <summary>
		/// Gets the major HTTP API <see cref="global::System.Version"/> number of the <see cref="SwarmNode"/>.
		/// </summary>
		public int MajorApiVersion => ApiHeaders.Version.Major;

		/// <summary>
		/// Gets the HTTP API <see cref="global::System.Version"/> of the <see cref="SwarmNode"/>.
		/// </summary>
		[TgsGraphQLAuthorize]
		public Version? ApiVersion => ApiHeaders.Version;

		/// <summary>
		/// Gets the DMAPI interop <see cref="global::System.Version"/> the <see cref="SwarmNode"/> uses.
		/// </summary>
		[TgsGraphQLAuthorize]
		public Version? DMApiVersion => DMApiConstants.InteropVersion;

		/// <summary>
		/// Gets the information needed to perform open authentication with the <see cref="SwarmNode"/>.
		/// </summary>
		/// <param name="oAuthProviders">The <see cref="IOAuthProviders"/> to use.</param>
		/// <returns>A map of enabled <see cref="OAuthProvider"/>s to their <see cref="OAuthProviderInfo"/>.</returns>
		public IReadOnlyDictionary<OAuthProvider, OAuthProviderInfo> OAuthProviderInfos(
			[Service] IOAuthProviders oAuthProviders)
		{
			ArgumentNullException.ThrowIfNull(oAuthProviders);
			return oAuthProviders.ProviderInfos();
		}
	}
}
