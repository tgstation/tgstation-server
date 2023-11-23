using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// An <see cref="IHubFilter"/> that denies method calls and connections if the <see cref="IAuthenticationContext"/> is not valid for an authorized user.
	/// </summary>
	sealed class AuthorizationContextHubFilter : IHubFilter
	{
		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the <see cref="AuthorizationContextHubFilter"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AuthorizationContextHubFilter"/>.
		/// </summary>
		readonly ILogger<AuthorizationContextHubFilter> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorizationContextHubFilter"/> class.
		/// </summary>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthorizationContextHubFilter(
			IAuthenticationContext authenticationContext,
			ILogger<AuthorizationContextHubFilter> logger)
		{
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
		{
			ArgumentNullException.ThrowIfNull(context);
			if (ValidateAuthenticationContext(context.Hub))
				await next(context);
		}

		/// <inheritdoc />
		public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
		{
			ArgumentNullException.ThrowIfNull(invocationContext);
			if (ValidateAuthenticationContext(invocationContext.Hub))
				return await next(invocationContext);

			return null;
		}

		/// <summary>
		/// Validates the <see cref="IAuthenticationContext"/> for the hub event.
		/// </summary>
		/// <param name="hub">The current <see cref="Hub"/>.</param>
		/// <returns><see langword="true"/> if the hub call should continue, <see langword="false"/> if it shouldn't and has been aborted.</returns>
		bool ValidateAuthenticationContext(Hub hub)
		{
			if (!authenticationContext.Valid)
				logger.LogTrace("The token for connection {connectionId} is no longer authenticated! Aborting...", hub.Context.ConnectionId);
			else if (!authenticationContext.User.Enabled.Value)
				logger.LogTrace("The token for connection {connectionId} is no longer authorized! Aborting...", hub.Context.ConnectionId);
			else
				return true;

			var hubType = hub.GetType();
			var allHubProperties = hubType.GetProperties();
			var typedClientsProperty = allHubProperties.Single(
				prop => prop.PropertyType.IsConstructedGenericType
					&& prop.Name == nameof(hub.Clients));
			var clients = typedClientsProperty.GetValue(hub);
			var callerProperty = clients.GetType().GetProperty(nameof(hub.Clients.Caller));
			var caller = callerProperty.GetValue(clients);

			hub.Context.Abort();
			return false;
		}
	}
}
