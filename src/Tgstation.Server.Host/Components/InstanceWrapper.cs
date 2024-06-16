using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// <see cref="ReferenceCounter{TInstance}"/> for a given <see cref="IInstance"/>.
	/// </summary>
	sealed class InstanceWrapper : ReferenceCounter<IInstance>, IInstanceReference
	{
		/// <summary>
		/// Static counter for <see cref="Uid"/>.
		/// </summary>
		static ulong instanceWrapperInstances;

		/// <inheritdoc />
		public ulong Uid { get; }

		/// <inheritdoc />
		public IRepositoryManager RepositoryManager => Instance.RepositoryManager;

		/// <inheritdoc />
		public IEngineManager EngineManager => Instance.EngineManager;

		/// <inheritdoc />
		public IDreamMaker DreamMaker => Instance.DreamMaker;

		/// <inheritdoc />
		public IWatchdog Watchdog => Instance.Watchdog;

		/// <inheritdoc />
		public IChatManager Chat => Instance.Chat;

		/// <inheritdoc />
		public IConfiguration Configuration => Instance.Configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceWrapper"/> class.
		/// </summary>
		public InstanceWrapper()
		{
			Uid = Interlocked.Increment(ref instanceWrapperInstances);
		}

		/// <inheritdoc />
		public ValueTask InstanceRenamed(string newInstanceName, CancellationToken cancellationToken) => Instance.InstanceRenamed(newInstanceName, cancellationToken);

		/// <inheritdoc />
		public ValueTask ScheduleAutoUpdate(uint newInterval, string? newCron) => Instance.ScheduleAutoUpdate(newInterval, newCron);

		/// <inheritdoc />
		public ValueTask<CompileJob?> LatestCompileJob() => Instance.LatestCompileJob();
	}
}
