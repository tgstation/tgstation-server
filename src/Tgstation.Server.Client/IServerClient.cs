using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Main client for communicating with a server
	/// </summary>
	public interface IServerClient : IDisposable
	{
		/// <summary>
		/// The <see cref="Token"/> being used to access the server
		/// </summary>
		Token Token { get; }

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
		/// The <see cref="ServerInformation"/> of the <see cref="IServerClient"/>
		/// </summary>
		Task<ServerInformation> Version(CancellationToken cancellationToken);

		/// <summary>
		/// Adds a <paramref name="requestLogger"/> to the request pipeline
		/// </summary>
		/// <param name="requestLogger">The <see cref="IRequestLogger"/> to add</param>
		void AddRequestLogger(IRequestLogger requestLogger);
	}
}
