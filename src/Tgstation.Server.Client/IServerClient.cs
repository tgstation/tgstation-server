using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Main client for communicating with a server
	/// </summary>
	public interface IServerClient : IDisposable
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="IServerClient"/>
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The connection timeout in milliseconds
		/// </summary>
		int Timeout { get; set; }

		/// <summary>
		/// The requery rate for job updates in milliseconds
		/// </summary>
		int RequeryRate { get; set; }

		/// <summary>
		/// Access the <see cref="ITokenClient"/>
		/// </summary>
		ITokenClient Tokens { get; }

		/// <summary>
		/// Access the <see cref="IInstanceManagerClient"/>
		/// </summary>
		IInstanceManagerClient Instances { get; }

		/// <summary>
		/// The <see cref="System.Version"/> of the connected server
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="System.Version"/> of the connected server</returns>
		/// <remarks>Note that if the <see cref="System.Version.Build"/> differs from the <see cref="Version"/>'s API functionality will most likely be compromised</remarks>
		Task<Version> GetServerVersion(CancellationToken cancellationToken);
	}
}
