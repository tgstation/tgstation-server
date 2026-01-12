using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstallationData"/> using a <see cref="IRepository"/>.
	/// </summary>
	sealed class RepositoryEngineInstallationData : IEngineInstallationData
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryEngineInstallationData"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The backing <see cref="IRepository"/>.
		/// </summary>
		readonly IRepository repository;

		/// <summary>
		/// The name of the subdirectory the <see cref="repository"/> is copied to.
		/// </summary>
		readonly string targetSubDirectory;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryEngineInstallationData"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="repository">The value of <see cref="repository"/>.</param>
		/// <param name="targetSubDirectory">The value of <see cref="targetSubDirectory"/>.</param>
		public RepositoryEngineInstallationData(IIOManager ioManager, IRepository repository, string targetSubDirectory)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
			this.targetSubDirectory = targetSubDirectory ?? throw new ArgumentNullException(nameof(targetSubDirectory));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync()
		{
			repository.Dispose();
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public ValueTask ExtractToPath(string path, CancellationToken cancellationToken)
			=> repository.CopyTo(
				ioManager.ConcatPath(
					path,
					targetSubDirectory),
				cancellationToken);
	}
}
