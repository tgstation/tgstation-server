using Microsoft.AspNetCore.Http;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Extension methods for <see cref="HttpContext"/>
	/// </summary>
	static class HttpContextExtensions
	{
		/// <summary>
		/// Get the <see cref="IAuthenticationContext"/> associated with the <see cref="HttpContext"/>
		/// </summary>
		/// <param name="httpContext">The <see cref="HttpContext"/> containing the <see cref="IAuthenticationContext"/></param>
		/// <returns>The <see cref="IAuthenticationContext"/> associated with the <see cref="HttpContext"/></returns>
		public static IAuthenticationContext AuthenticationContext(this HttpContext httpContext) => Security.AuthenticationContext.Current(httpContext);
	}
}
