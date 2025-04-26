using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// <see cref="AuthenticationHandler{TOptions}"/> for the swarm protocol.
	/// </summary>
	sealed class SwarmAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="SwarmConfiguration"/>.
		/// </summary>
		readonly IOptionsMonitor<SwarmConfiguration> swarmConfigurationOptions;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmAuthenticationHandler"/> class.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The value of <see cref="swarmConfigurationOptions"/>.</param>
		/// <param name="authenticationSchemeOptions">The <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="AuthenticationSchemeOptions"/> to use.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
		/// <param name="encoder">The <see cref="UrlEncoder"/> to use.</param>
		public SwarmAuthenticationHandler(
			IOptionsMonitor<SwarmConfiguration> swarmConfigurationOptions,
			IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
			ILoggerFactory loggerFactory,
			UrlEncoder encoder)
			: base(
				  authenticationSchemeOptions,
				  loggerFactory,
				  encoder)
		{
			this.swarmConfigurationOptions = swarmConfigurationOptions ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
		}

		/// <inheritdoc />
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authHeader = Context.Request.Headers.Authorization;
			if (authHeader.Count != 1)
			{
				return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header!"));
			}

			var splits = (authHeader[0] ?? String.Empty).Split(" ");
			if (splits.Length < 2)
			{
				return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header!"));
			}

			var scheme = splits[0];
			var token = String.Join(" ", splits.Skip(1));

			if (scheme != Scheme.Name)
			{
				throw new InvalidOperationException("Invalid scheme!");
			}

			if (token != swarmConfigurationOptions.CurrentValue.PrivateKey)
			{
				return Task.FromResult(AuthenticateResult.Fail("Unauthorized swarm private key!"));
			}

			return Task.FromResult(
				AuthenticateResult.Success(
					new AuthenticationTicket(
						new ClaimsPrincipal(
							new ClaimsIdentity(SwarmConstants.AuthenticationSchemeAndPolicy)),
						Scheme.Name)));
		}
	}
}
