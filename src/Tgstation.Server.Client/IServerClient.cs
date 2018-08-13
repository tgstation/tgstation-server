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
		/// The <see cref="System.Version"/> of the <see cref="IServerClient"/>
		/// </summary>
		Task<Version> Version(CancellationToken cancellationToken);

		/// <summary>
		/// The connection timeout in milliseconds
		/// </summary>
		int Timeout { get; set; }

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
		/// Creates a <see cref="Task{TResult}"/> that completes when a given <paramref name="job"/> is completed
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to create a <see cref="Task"/> for</param>
		/// <param name="requeryRate">The rate in seconds to poll the server for results</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> which will trigger the cancellation of the <paramref name="job"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a complete <see cref="Job"/></returns>
		Task<Job> CreateTaskFromJob(Job job, int requeryRate, CancellationToken cancellationToken);
	}
}
