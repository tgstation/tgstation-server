using System.Collections.Generic;

using Microsoft.AspNetCore.Authorization;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="IAuthorizationRequirement"/> for testing if a user is enabled and their session is valid.
	/// </summary>
	sealed class UserSessionValidRequirement : IAuthorizationRequirement
	{
		/// <summary>
		/// The singleton instance of this class.
		/// </summary>
		public static IEnumerable<UserSessionValidRequirement> InstanceAsEnumerable { get; } =
		[
			new(),
		];

		/// <summary>
		/// Initializes a new instance of the <see cref="UserSessionValidRequirement"/> class.
		/// </summary>
		private UserSessionValidRequirement()
		{
		}
	}
}
