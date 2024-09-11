using System;

using HotChocolate;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// <see cref="IGateway"/> for accessing remote <see cref="Node"/>s.
	/// </summary>
	/// <remarks>This is currently unimplemented.</remarks>
	public sealed class RemoteGateway : IGateway
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

			throw new ErrorMessageException(ErrorCode.RemoteGatewaysNotImplemented);
		}
	}
}
