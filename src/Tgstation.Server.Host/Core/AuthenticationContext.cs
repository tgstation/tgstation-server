using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;

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

                ISystemIdentity systemIdentity;

				var systemIdentityFactory = applicationBuilder.ApplicationServices.GetRequiredService<ISystemIdentityFactory>();
				var tokenManager = applicationBuilder.ApplicationServices.GetRequiredService<ITokenManager>();
				var databaseContext = applicationBuilder.ApplicationServices.GetRequiredService<IDatabaseContext>();
				try
                {
					if (headers.IsTokenAuthentication)
					{
						var user = await tokenManager.GetUser(headers.Token, httpContext.RequestAborted).ConfigureAwait(false);
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
		/// <param name="systemIdentity">The value of <see cref="systemIdentity"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public AuthenticationContext(ISystemIdentity systemIdentity, IDatabaseContext databaseContext)
		{
			SystemIdentity = systemIdentity ?? throw new ArgumentNullException(nameof(systemIdentity));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity.Dispose();

		/// <inheritdoc />
		public Task<User> User(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<Api.Models.InstanceUser> InstanceUser(Instance instance, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
	}
}
