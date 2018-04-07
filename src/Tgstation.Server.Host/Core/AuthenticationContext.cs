using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using System;
using System.Net;
using System.Linq;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	sealed class AuthenticationContext : IAuthenticationContext
	{
		static readonly object contextKey = new object();

		public static IAuthenticationContext Current(HttpContext httpContext) => (IAuthenticationContext)httpContext.Items[contextKey];

		public static void AddToPipeline(IApplicationBuilder applicationBuilder) => applicationBuilder.Use(async (httpContext, next) =>
		{
			Headers headers;
			try
			{
				headers = new Headers(httpContext.Request.Headers.ToDictionary(x => x.Key, x => x.Value.First()));
			}
			catch (InvalidOperationException)
			{
				httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				return;
			}

			using (var authContext = new AuthenticationContext(headers))
			{
				if (!authContext.Valid)
				{
					httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
					return;
				}
				httpContext.Items[contextKey] = authContext;
				await next().ConfigureAwait(false);
			}
		});

		public AuthenticationContext(Headers headers)
		{
			throw new NotImplementedException();
		}

		~AuthenticationContext() => Dispose();

		public void Dispose()
		{

			GC.SuppressFinalize(this);
		}

		public IAuthenticationContext Clone() => throw new NotImplementedException();

		bool Valid => throw new NotImplementedException();

		public User User => throw new NotImplementedException();

		public InstanceUser InstanceUser => throw new NotImplementedException();
	}
}
