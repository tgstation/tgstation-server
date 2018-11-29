using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For interacting with the instance services
	/// </summary>
	public interface IInstance : IHostedService, IDisposable
	{
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="IInstance"/>
		/// </summary>
		IRepositoryManager RepositoryManager { get; }

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="IInstance"/>
		/// </summary>
		IByondManager ByondManager { get; }

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="IInstance"/>
		/// </summary>
		IWatchdog Watchdog { get; }

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="IInstance"/>
		/// </summary>
		IChat Chat { get; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="IInstance"/>
		/// </summary>
		IConfiguration Configuration { get; }

		/// <summary>
		/// The latest staged <see cref="CompileJob"/>
		/// </summary>
		/// <returns>The latest <see cref="CompileJob"/> if it exists</returns>
		CompileJob LatestCompileJob();

		/// <summary>
		/// Rename the <see cref="IInstance"/>
		/// </summary>
		/// <param name="newName">The new name for the <see cref="IInstance"/></param>
		void Rename(string newName);

		/// <summary>
		/// Change the <see cref="Api.Models.Instance.AutoUpdateInterval"/> for the <see cref="IInstance"/>
		/// </summary>
		/// <param name="newInterval">The new auto update inteval</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetAutoUpdateInterval(uint newInterval);

		/// <summary>
		/// Run the compile job and insert it into the database. Meant to be called by a <see cref="Core.IJobManager"/>
		/// </summary>
		/// <param name="job">The running <see cref="Job"/></param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the operation</param>
		/// <param name="progressReporter">The <see cref="Action{T1}"/> to report compilation progress</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CompileProcess(Job job, IDatabaseContext databaseContext, Action<int> progressReporter, CancellationToken cancellationToken);
	}
}