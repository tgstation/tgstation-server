using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
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
		/// The <see cref="IDreamMaker"/> for the <see cref="IInstance"/>
		/// </summary>
		IDreamMaker DreamMaker { get; }

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="IInstance"/>
		/// </summary>
		IWatchdog Watchdog { get; }

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="IInstance"/>
		/// </summary>
		IChat Chat { get; }

		/// <summary>
		/// The <see cref="ICompileJobConsumer"/> for the <see cref="IInstance"/>
		/// </summary>
		ICompileJobConsumer CompileJobConsumer { get; }

		/// <summary>
		/// The <see cref="StaticFiles.IConfiguration"/> for the <see cref="IInstance"/>
		/// </summary>
		IConfiguration Configuration { get; }

		/// <summary>
		/// The latest staged <see cref="CompileJob"/>
		/// </summary>
		/// <returns>The latest <see cref="CompileJob"/> if it exists</returns>
		CompileJob LatestCompileJob();

		/// <summary>
		/// Get the <see cref="Api.Models.Instance"/> associated with the <see cref="IInstance"/>
		/// </summary>
		/// <returns>The <see cref="Api.Models.Instance"/> associated with the <see cref="IInstance"/></returns>
		Api.Models.Instance GetMetadata();

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
	}
}