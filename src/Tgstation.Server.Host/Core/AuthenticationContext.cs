using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Manages <see cref="Api.Models.User"/>s for a scope
	/// </summary>
	sealed class AuthenticationContext : IAuthenticationContext
	{
		/// <summary>
		/// Used to store the <see cref="AuthenticationContext"/> in <see cref="HttpContext.Items"/>
		/// </summary>
		static readonly object contextKey = new object();

		/// <summary>
		/// Get the current <see cref="IAuthenticationContext"/> from an <paramref name="httpContext"/>
		/// </summary>
		/// <param name="httpContext">The <see cref="HttpContext"/> containing the <see cref="IAuthenticationContext"/></param>
		/// <returns>The <see cref="IAuthenticationContext"/> in <paramref name="httpContext"/></returns>
		public static IAuthenticationContext Current(HttpContext httpContext) => (IAuthenticationContext)httpContext.Items[contextKey];

		/// <summary>
		/// Adds <see cref="AuthenticationContext"/> handling to an <paramref name="applicationBuilder"/> pipeline
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add to</param>
        public static void AddToPipeline(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Use(async (httpContext, next) =>
            {
                ApiHeaders headers;
                try
                {
                    headers = new ApiHeaders(httpContext.Request.GetTypedHeaders());
                }
                catch (InvalidOperationException)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                ISystemIdentity systemIdentity;

				var systemIdentityFactory = applicationBuilder.ApplicationServices.GetRequiredService<ISystemIdentityFactory>();
				var tokenManager = applicationBuilder.ApplicationServices.GetRequiredService<ITokenManager>();
				var databaseContext = applicationBuilder.ApplicationServices.GetRequiredService<IDatabaseContext>();
				try
                {
					if (headers.IsTokenAuthentication)
					{
						var user = await tokenManager.GetUser(new Token { Value = headers.Token }, httpContext.RequestAborted).ConfigureAwait(false);
						systemIdentity = systemIdentityFactory.CreateSystemIdentity(user);
					}
					else
						systemIdentity = systemIdentityFactory.CreateSystemIdentity(headers.Username, headers.Password);
                }
                catch (UnauthorizedAccessException)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }

                using (var authContext = new AuthenticationContext(systemIdentity, databaseContext))
                {
                    httpContext.Items[contextKey] = authContext;
                    await next().ConfigureAwait(false);
                }
            });
		}

		/// <inheritdoc />
		public ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="AuthenticationContext"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// Construct a <see cref="IAuthenticationContext"/>
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public AuthenticationContext(ISystemIdentity systemIdentity, IDatabaseContext databaseContext)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			SystemIdentity = systemIdentity;
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity.Dispose();

		/// <inheritdoc />
		public Task<Models.User> User(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<Models.InstanceUser> InstanceUser(Models.Instance instance, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
	}
}
