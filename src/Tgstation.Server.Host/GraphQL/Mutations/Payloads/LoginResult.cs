using HotChocolate;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.GraphQL.Scalars;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.GraphQL.Mutations.Payloads
{
	/// <summary>
	/// Success response for a login attempt.
	/// </summary>
	public sealed class LoginResult : ILegacyApiTransformable<TokenResponse>
	{
		/// <summary>
		/// The JSON Web Token (JWT) to use as a Bearer token for accessing the server at non-login endpoints. Contains an expiry time.
		/// </summary>
		[GraphQLType<JwtType>]
		[GraphQLNonNullType]
		public required string Bearer { get; init; }

		/// <summary>
		/// The <see cref="User"/> that was logged in.
		/// </summary>
		public required Types.User User { get; init; }

		/// <inheritdoc />
		[GraphQLIgnore]
		public TokenResponse ToApi()
			=> new()
			{
				Bearer = Bearer,
			};
	}
}
