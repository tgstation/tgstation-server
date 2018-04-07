using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tgstation.Server.Host.Core
{
	static class ApplicationBuilderExtensions
	{
        public static IApplicationBuilder UseSystemAuthentication(this IApplicationBuilder applicationBuilder)
        {
            AuthenticationContext.AddToPipeline(applicationBuilder);
            return applicationBuilder;
        }
	}
}
