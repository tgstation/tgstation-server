using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// Helper for authorization functionality related to GraphQL.
	/// </summary>
	static class AuthorizationHelper
	{
		/// <summary>
		/// Create a new <see cref="GraphQLException"/> to be thrown when a forbidden error occurs.
		/// </summary>
		/// <param name="authorizationFailure">The <see cref="AuthorizationFailure"/>.</param>
		/// <returns>A new <see cref="GraphQLException"/>.</returns>
		public static GraphQLException ForbiddenGraphQLException(this AuthorizationFailure authorizationFailure)
		{
			ArgumentNullException.ThrowIfNull(authorizationFailure);

			var messageBuilder = new StringBuilder("The current user is not authorized to access this resource.");

			foreach (var failureReason in authorizationFailure.FailureReasons)
			{
				messageBuilder.AppendLine();
				messageBuilder.Append("\t- ");
				messageBuilder.Append(failureReason.Message);
			}

			return new(ErrorBuilder.New()
				.SetMessage(messageBuilder.ToString()) // Copied from graphql-platform: AuthorizeMiddleware.cs
				.SetCode(ErrorCodes.Authentication.NotAuthorized)
				.Build());
		}

		/// <summary>
		/// Evaluate a given set of <paramref name="authorizationRequirements"/>, throwing the approriate <see cref="GraphQLException"/> on failure.
		/// </summary>
		/// <param name="authorizationService">The authorization service to use.</param>
		/// <param name="authorizationRequirements">The <see cref="IEnumerable{T}"/> of <see cref="IAuthorizationRequirement"/>s to evaluate..</param>
		/// <param name="excludeUserSessionValidRequirement">If the <see cref="UserSessionValidRequirement"/> should be excluded.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		public static async ValueTask CheckGraphQLAuthorized(
			this Security.IAuthorizationService authorizationService,
			IEnumerable<IAuthorizationRequirement>? authorizationRequirements,
			bool excludeUserSessionValidRequirement = false)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(authorizationRequirements);

			if (!excludeUserSessionValidRequirement)
				authorizationRequirements = UserSessionValidRequirement.InstanceAsEnumerable.Concat(authorizationRequirements);

			var result = await authorizationService.AuthorizeAsync(authorizationRequirements);
			if (!result.Succeeded)
				throw result.Failure.ForbiddenGraphQLException();
		}
	}
}
