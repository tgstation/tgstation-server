using System;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// <see cref="IGateway"/> for the <see cref="Node"/> this query is executing on.
	/// </summary>
	public sealed class LocalGateway : IGateway
	{
		/// <inheritdoc />
		public GatewayInformation Information(
			[Service] IOAuthProviders oAuthProviders,
			[Service] IPlatformIdentifier platformIdentifier,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(oAuthProviders);
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			ArgumentNullException.ThrowIfNull(generalConfigurationOptions);

			var generalConfiguration = generalConfigurationOptions.Value;
			return new GatewayInformation
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
