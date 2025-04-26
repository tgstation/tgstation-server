using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Security.RightsEvaluation;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="AuthorizationHandler{TRequirement}"/> for <see cref="RightsConditional{TRights}"/>s.
	/// </summary>
	sealed class UserSessionAuthorizationHandler : AuthorizationHandler<UserSessionValidRequirement>
	{
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="RightsAuthorizationHandler{TRights}"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserSessionAuthorizationHandler"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		public UserSessionAuthorizationHandler(IDatabaseContext databaseContext)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserSessionValidRequirement requirement)
		{
			// https://github.com/dotnet/aspnetcore/issues/56272
			CancellationToken cancellationToken = CancellationToken.None;

			var userId = context.User.GetTgsUserId();


			if ()
				context.Succeed(requirement);
			else
				context.Fail(
					new AuthorizationFailureReason(this, $"Failed to successfully evaluate rights requirement: {requirement}"));
		}
	}
}
