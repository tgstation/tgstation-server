using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Tgstation.Server.Host.Security;

#nullable disable

namespace Tgstation.Server.Host.Utils.SignalR
{
	/// <summary>
	/// Base <see langword="class"/> for <see cref="Hub{T}"/>s that want to map their connection IDs to <see cref="Models.PermissionSet"/>s.
	/// </summary>
	/// <typeparam name="TChildHub">The child <see langword="class"/> inheriting from the <see cref="ConnectionMappingHub{TChildHub, THubMethods}"/>.</typeparam>
	/// <typeparam name="THubMethods">The <see langword="interface"/> for implementing <see cref="Hub{T}"/> methods.</typeparam>
	[TgsAuthorize]
	abstract class ConnectionMappingHub<TChildHub, THubMethods> : Hub<THubMethods>
		where TChildHub : ConnectionMappingHub<TChildHub, THubMethods>
		where THubMethods : class
	{
		/// <summary>
		/// The <see cref="IHubConnectionMapper{THub, THubMethods}"/> used to map connections.
		/// </summary>
		readonly IHubConnectionMapper<TChildHub, THubMethods> connectionMapper;

		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the <see cref="ConnectionMappingHub{TChildHub, THubMethods}"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionMappingHub{TParentHub, TClientMethods}"/> class.
		/// </summary>
		/// <param name="connectionMapper">The value of <see cref="connectionMapper"/>.</param>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		protected ConnectionMappingHub(
			IHubConnectionMapper<TChildHub, THubMethods> connectionMapper,
			IAuthenticationContext authenticationContext)
		{
			this.connectionMapper = connectionMapper ?? throw new ArgumentNullException(nameof(connectionMapper));
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
		}

		/// <inheritdoc />
		public override async Task OnConnectedAsync()
		{
			await connectionMapper.UserConnected(authenticationContext, (TChildHub)this, Context.ConnectionAborted);
			await base.OnConnectedAsync();
		}

		/// <inheritdoc />
		[AllowAnonymous]
		public override Task OnDisconnectedAsync(Exception exception)
		{
			connectionMapper.UserDisconnected(Context.ConnectionId);
			return base.OnDisconnectedAsync(exception);
		}
	}
}
