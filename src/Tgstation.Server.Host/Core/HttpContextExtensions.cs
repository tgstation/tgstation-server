using Microsoft.AspNetCore.Http;

namespace Tgstation.Server.Host.Core
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
		public static IAuthenticationContext AuthenticationContext(this HttpContext httpContext) => Core.AuthenticationContext.Current(httpContext);
	}
}
