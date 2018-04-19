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
		/// The connection timeout in milliseconds. Defaults to 10000
		/// </summary>
		int Timeout { get; set; }

		/// <summary>
		/// How long to return initially cached models for in seconds. Defaults to 60
		/// </summary>
		int CacheExpiry { get; set; }

		/// <summary>
		/// The requery rate for job updates in milliseconds. Defaults to 5000
		/// </summary>
		int RequeryRate { get; set; }

		/// <summary>
		/// Access the <see cref="IInstanceManagerClient"/>
		/// </summary>
		IInstanceManagerClient Instances { get; }

		/// <summary>
		/// Access the <see cref="IAdministrationClient"/>
		/// </summary>
		IAdministrationClient Administration { get; }

		/// <summary>
		/// These generally shouldn't be used in favor of the <see cref="Task"/> based polling and <see cref="CancellationToken"/>s other clients use. However, this is the only way to access jobs the client didn't start in it's current session
		/// </summary>
		IJobsClient Jobs { get; }

		/// <summary>
		/// The <see cref="System.Version"/> of the connected server
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="System.Version"/> of the connected server</returns>
		/// <remarks>Note that if the <see cref="System.Version.Build"/> differs from the <see cref="Version"/>'s API functionality will most likely be compromised</remarks>
		Task<Version> GetServerVersion(CancellationToken cancellationToken);
	}
}
