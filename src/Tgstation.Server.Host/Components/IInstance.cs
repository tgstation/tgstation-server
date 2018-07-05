using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For interacting with the instance services
	/// </summary>
	public interface IInstance : IHostedService
	{
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="IInstance"/>
		/// </summary>
		IRepositoryManager RepositoryManager { get; }

		/// <summary>
		/// The <see cref="IByond"/> for the <see cref="IInstance"/>
		/// </summary>
		IByond Byond { get; }

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
		/// The <see cref="IConfiguration"/> for the <see cref="IInstance"/>
		/// </summary>
		IConfiguration Configuration { get; }

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
		Task SetAutoUpdateInterval(int? newInterval);
	}
}