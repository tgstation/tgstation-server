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

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Warpper around a given <see cref="IInstance"/> with a <see cref="Dispose"/> <see cref="Action"/>.
	/// </summary>
	sealed class InstanceWrapper : IInstanceReference
	{
		/// <inheritdoc />
		public Guid Uid { get; }

		/// <summary>
		/// The <see langword="lock"/> object for <see cref="Dispose"/>.
		/// </summary>
		readonly object disposeLock;

		/// <summary>
		/// The <see cref="Action"/> to take when <see cref="Dispose"/> is called.
		/// </summary>
		Action? onDisposed;

		/// <summary>
		/// The <see cref="IInstance"/> calls are forwarded to.
		/// </summary>
		IInstanceCore? actualInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceWrapper"/> class.
		/// </summary>
		/// <param name="actualInstance">The value of <see cref="actualInstance"/>.</param>
		/// <param name="onDisposed">The value of <see cref="onDisposed"/>.</param>
		public InstanceWrapper(IInstanceCore actualInstance, Action onDisposed)
		{
			this.actualInstance = actualInstance ?? throw new ArgumentNullException(nameof(actualInstance));
			this.onDisposed = onDisposed ?? throw new ArgumentNullException(nameof(onDisposed));
			Uid = Guid.NewGuid();
			disposeLock = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (disposeLock)
			{
				onDisposed?.Invoke();
				onDisposed = null;
				actualInstance = null;
			}
		}

		/// <inheritdoc />
		public IRepositoryManager RepositoryManager => actualInstance?.RepositoryManager ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public IByondManager ByondManager => actualInstance?.ByondManager ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public IDreamMaker DreamMaker => actualInstance?.DreamMaker ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public IWatchdog Watchdog => actualInstance?.Watchdog ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public IChatManager Chat => actualInstance?.Chat ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public IConfiguration Configuration => actualInstance?.Configuration ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
			=> actualInstance?.InstanceRenamed(newInstanceName, cancellationToken) ?? throw new ObjectDisposedException(nameof(InstanceWrapper));

		/// <inheritdoc />
		public CompileJob? LatestCompileJob()
		{
			if (actualInstance == null)
				throw new ObjectDisposedException(nameof(InstanceWrapper));
			return actualInstance.LatestCompileJob();
		}

		/// <inheritdoc />
		public Task SetAutoUpdateInterval(uint newInterval)
		{
			if (actualInstance == null)
				throw new ObjectDisposedException(nameof(InstanceWrapper));
			return actualInstance.SetAutoUpdateInterval(newInterval);
		}
	}
}
