using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
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
		/// <inheritdoc />
		public Guid Uid { get; }

		/// <inheritdoc />
		public IRepositoryManager RepositoryManager => Instance.RepositoryManager;

		/// <inheritdoc />
		public IByondManager ByondManager => Instance.ByondManager;

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
			Uid = Guid.NewGuid();
		}

		/// <inheritdoc />
		public ValueTask InstanceRenamed(string newInstanceName, CancellationToken cancellationToken) => Instance.InstanceRenamed(newInstanceName, cancellationToken);

		/// <inheritdoc />
		public ValueTask SetAutoUpdateInterval(uint newInterval) => Instance.SetAutoUpdateInterval(newInterval);

		/// <inheritdoc />
		public CompileJob LatestCompileJob() => Instance.LatestCompileJob();
	}
}
