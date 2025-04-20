using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="IAuthorizationRequirement"/> for testing if a user is enabled and their session is valid.
	/// </summary>
	sealed class UserSessionValidRequirement : IAuthorizationRequirement
	{
	}
}
