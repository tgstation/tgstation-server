using HotChocolate;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.GraphQL.Mutations
{
	/// <summary>
	/// Success response for a login attempt.
	/// </summary>
	public sealed class LoginPayload : ILegacyApiTransformable<TokenResponse>
	{
		/// <summary>
		/// The JSON Web Token (JWT) to use as a Bearer token for accessing the server. Contains an expiry time.
		/// </summary>
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
