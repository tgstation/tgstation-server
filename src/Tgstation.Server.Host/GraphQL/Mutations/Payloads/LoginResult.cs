using HotChocolate;
using Tgstation.Server.Host.GraphQL.Scalars;

namespace Tgstation.Server.Host.GraphQL.Mutations.Payloads
{
	/// <summary>
	/// Success response for a login attempt.
	/// </summary>
	public sealed class LoginResult
	{
		/// <summary>
		/// The JSON Web Token (JWT) to use as a Bearer token for accessing the server at non-login endpoints. Contains an expiry time.
		/// </summary>
		[GraphQLType<JwtType>]
		[GraphQLNonNullType]
		public required string Bearer { get; init; }
	}
}
