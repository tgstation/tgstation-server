using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tgstation.Server.Host.Core
{
	static class HttpContextExtensions
	{
        public static IAuthenticationContext AuthenticationContext(this HttpContext httpContext) => Core.AuthenticationContext.Current(httpContext);
	}
}
