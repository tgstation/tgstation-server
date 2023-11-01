using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Utils.SignalR
{
	/// <summary>
	/// Handles mapping connection IDs to <see cref="User"/>s for a given <typeparamref name="THub"/>.
	/// </summary>
	/// <typeparam name="THub">The <see cref="Hub"/> whose connections are being mapped.</typeparam>
	/// <typeparam name="THubMethods">The interface <see cref="IErrorHandlingHub"/> for implementing <see cref="Hub{T}"/> methods.</typeparam>
	interface IHubConnectionMapper<THub, THubMethods>
		where THub : ConnectionMappingHub<THub, THubMethods>
		where THubMethods : class, IErrorHandlingHub
	{
		/// <summary>
		/// To be called when a hub connection is made.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> associated with the connection.</param>
		/// <param name="hub">The <typeparamref name="THub"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask UserConnected(IAuthenticationContext authenticationContext, THub hub, CancellationToken cancellationToken);

		/// <summary>
		/// To be called when a hub connection is terminated.
		/// </summary>
		/// <param name="connectionId">The connection ID.</param>
		void UserDisconnected(string connectionId);
	}
}
