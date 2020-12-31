﻿using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For interacting with the instance services
	/// </summary>
	public interface IInstanceCore : ILatestCompileJobProvider, IRenameNotifyee
	{
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		IRepositoryManager RepositoryManager { get; }

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		IByondManager ByondManager { get; }

		/// <summary>
		/// The <see cref="IDreamMaker"/> for the <see cref="IInstanceCore"/>.
		/// </summary>
		IDreamMaker DreamMaker { get; }

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		IWatchdog Watchdog { get; }

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		IChatManager Chat { get; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		IConfiguration Configuration { get; }

		/// <summary>
		/// Change the <see cref="Api.Models.Instance.AutoUpdateInterval"/> for the <see cref="IInstanceCore"/>
		/// </summary>
		/// <param name="newInterval">The new auto update inteval</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetAutoUpdateInterval(uint newInterval);
	}
}