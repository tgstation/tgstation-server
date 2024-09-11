using System;

using HotChocolate;
using HotChocolate.Authorization;

using Microsoft.Extensions.Options;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents the local tgstation-server.
	/// </summary>
	public sealed class LocalServer
	{
		/// <summary>
		/// Gets <see cref="LocalServerInformation"/>.
		/// </summary>
		/// <param name="oAuthProviders">The <see cref="IOAuthProviders"/> to use.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use.</param>
		/// <returns>A new <see cref="LocalServerInformation"/>.</returns>
		[AllowAnonymous]
		public LocalServerInformation Information(
			[Service] IOAuthProviders oAuthProviders,
			[Service] IPlatformIdentifier platformIdentifier,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(oAuthProviders);
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			var generalConfiguration = generalConfigurationOptions.Value;
			return new LocalServerInformation
			{
				MinimumPasswordLength = generalConfiguration.MinimumPasswordLength,
				InstanceLimit = generalConfiguration.InstanceLimit,
				UserLimit = generalConfiguration.UserLimit,
				UserGroupLimit = generalConfiguration.UserGroupLimit,
				ValidInstancePaths = generalConfiguration.ValidInstancePaths,
				WindowsHost = platformIdentifier.IsWindows,
				OAuthProviderInfos = oAuthProviders.ProviderInfos(),
			};
		}
	}
}
