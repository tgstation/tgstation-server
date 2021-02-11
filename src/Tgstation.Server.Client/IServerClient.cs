using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Main client for communicating with a server
	/// </summary>
	public interface IServerClient : IDisposable
	{
		/// <summary>
		/// The connected server <see cref="Uri"/>
		/// </summary>
		Uri Url { get; }

		/// <summary>
		/// The <see cref="Token"/> used to access the server
		/// </summary>
		TokenResponse Token { get; set; }

		/// <summary>
		/// The connection timeout
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// Access the <see cref="IInstanceManagerClient"/>
		/// </summary>
		IInstanceManagerClient Instances { get; }

		/// <summary>
		/// Access the <see cref="IAdministrationClient"/>
		/// </summary>
		IAdministrationClient Administration { get; }

		/// <summary>
		/// Access the <see cref="IUsersClient"/>
		/// </summary>
		IUsersClient Users { get; }

		/// <summary>
		/// Access the <see cref="IUserGroupsClient"/>.
		/// </summary>
		IUserGroupsClient Groups { get; }

		/// <summary>
		/// The <see cref="ServerInformationResponse"/> of the <see cref="IServerClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerInformationResponse"/> of the target server</returns>
		Task<ServerInformationResponse> ServerInformation(CancellationToken cancellationToken);

		/// <summary>
		/// Adds a <paramref name="requestLogger"/> to the request pipeline
		/// </summary>
		/// <param name="requestLogger">The <see cref="IRequestLogger"/> to add</param>
		void AddRequestLogger(IRequestLogger requestLogger);
	}
}
