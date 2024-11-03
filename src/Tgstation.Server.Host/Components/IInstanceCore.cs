using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For interacting with the instance services.
	/// </summary>
	public interface IInstanceCore : ILatestCompileJobProvider, IRenameNotifyee
	{
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IRepositoryManager RepositoryManager { get; }

		/// <summary>
		/// The <see cref="IEngineManager"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IEngineManager EngineManager { get; }

		/// <summary>
		/// The <see cref="IDreamMaker"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IDreamMaker DreamMaker { get; }

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IWatchdog Watchdog { get; }

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IChatManager Chat { get; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IConfiguration Configuration { get; }

		/// <summary>
		/// Change the auto-update timing for the <see cref="IInstanceCore"/>.
		/// </summary>
		/// <param name="newInterval">The new auto-update inteval.</param>
		/// <param name="newCron">The new auto-update cron schedule.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ScheduleAutoUpdate(uint newInterval, string? newCron);

		/// <summary>
		/// Change the server auto-start timing for the <see cref="IInstanceCore"/>.
		/// </summary>
		/// <param name="newCron">The new auto-start cron schedule.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ScheduleServerStart(string? newCron);

		/// <summary>
		/// Change the server auto-stop timing for the <see cref="IInstanceCore"/>.
		/// </summary>
		/// <param name="newCron">The new auto-stop cron schedule.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ScheduleServerStop(string? newCron);
	}
}
