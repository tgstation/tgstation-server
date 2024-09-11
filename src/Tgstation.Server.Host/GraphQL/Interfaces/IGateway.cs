using HotChocolate;
using HotChocolate.Authorization;

using Microsoft.Extensions.Options;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Interfaces
{
	/// <summary>
	/// Management interface for the parent <see cref="Node"/>.
	/// </summary>
	public interface IGateway
	{
		/// <summary>
		/// Gets <see cref="GatewayInformation"/>.
		/// </summary>
		/// <param name="oAuthProviders">The <see cref="IOAuthProviders"/> to use.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use.</param>
		/// <returns>A new <see cref="GatewayInformation"/>.</returns>
		[AllowAnonymous]
		GatewayInformation Information(
			[Service] IOAuthProviders oAuthProviders,
			[Service] IPlatformIdentifier platformIdentifier,
			[Service] IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions);
	}
}
