using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Client.Rights;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// <see cref="IClient{TRights}"/> for communicating with a server
	/// </summary>
	public interface IServerClient : IDisposable, IClient<ServerRights>
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="IServerClient"/>
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// Access the <see cref="ITokenClient"/>
		/// </summary>
		ITokenClient Token { get; }

		/// <summary>
		/// Access the <see cref="IInstanceManagerClient"/>
		/// </summary>
		IInstanceManagerClient Instance { get; }

		/// <summary>
		/// The <see cref="System.Version"/> of the connected server
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="System.Version"/> of the connected server</returns>
		/// <remarks>Note that if the <see cref="System.Version.Build"/> differs from the <see cref="Version"/>'s API functionality will most likely be compromised</remarks>
		Task<Version> GetServerVersion(CancellationToken cancellationToken);
	}
}
