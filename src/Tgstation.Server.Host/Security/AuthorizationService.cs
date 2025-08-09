using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthorizationService : IAuthorizationService
	{
		/// <summary>
		/// The <see cref="IHttpContextAccessor"/> for the <see cref="AuthorizationService"/>.
		/// </summary>
		readonly IClaimsPrincipalAccessor claimsPrincipalAccessor;

		/// <summary>
		/// The <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService"/> for the <see cref="AuthorizationService"/>.
		/// </summary>
		readonly Microsoft.AspNetCore.Authorization.IAuthorizationService aspNetCoreAuthorizationService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorizationService"/> class.
		/// </summary>
		/// <param name="claimsPrincipalAccessor">The value of <see cref="claimsPrincipalAccessor"/>.</param>
		/// <param name="aspNetCoreAuthorizationService">The value of <see cref="aspNetCoreAuthorizationService"/>.</param>
		public AuthorizationService(
			IClaimsPrincipalAccessor claimsPrincipalAccessor,
			Microsoft.AspNetCore.Authorization.IAuthorizationService aspNetCoreAuthorizationService)
		{
			this.claimsPrincipalAccessor = claimsPrincipalAccessor ?? throw new ArgumentNullException(nameof(claimsPrincipalAccessor));
			this.aspNetCoreAuthorizationService = aspNetCoreAuthorizationService ?? throw new ArgumentNullException(nameof(aspNetCoreAuthorizationService));
		}

		/// <inheritdoc />
		public async ValueTask<AuthorizationResult> AuthorizeAsync(IEnumerable<IAuthorizationRequirement> requirements, object? resource)
		{
			ArgumentNullException.ThrowIfNull(requirements);

			// asp net fails for an empty authorization requirement list
			var bakedRequirements = requirements.ToList();
			if (bakedRequirements.Count == 0)
				return AuthorizationResult.Success();

			var result = await aspNetCoreAuthorizationService.AuthorizeAsync(
				claimsPrincipalAccessor.User,
				resource,
				bakedRequirements);

			return result;
		}
	}
}
