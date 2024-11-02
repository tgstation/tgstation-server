using HotChocolate;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.GraphQL.Mutations.Payloads
{
	/// <summary>
	/// Success result for an OAuth gateway login attempt.
	/// </summary>
	public sealed class OAuthGatewayLoginResult : ILegacyApiTransformable<OAuthGatewayResponse>
	{
		/// <summary>
		/// The user's access token for the requested OAuth service.
		/// </summary>
		public required string AccessCode { get; init; }

		/// <inheritdoc />
		[GraphQLIgnore]
		public OAuthGatewayResponse ToApi()
			=> new()
			{
				AccessCode = AccessCode,
			};
	}
}
