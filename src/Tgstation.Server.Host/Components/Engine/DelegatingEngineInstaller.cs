using System;
using System.Collections.Frozen;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstaller"/> that forwards calls to different <see cref="IEngineInstaller"/> based on their appropriate <see cref="EngineType"/>.
	/// </summary>
	sealed class DelegatingEngineInstaller : IEngineInstaller
	{
		/// <summary>
		/// The <see cref="FrozenDictionary{TKey, TValue}"/> mapping <see cref="EngineType"/>s to their appropriate <see cref="IEngineInstaller"/>.
		/// </summary>
		readonly FrozenDictionary<EngineType, IEngineInstaller> delegatedInstallers;

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegatingEngineInstaller"/> class.
		/// </summary>
		/// <param name="delegatedInstallers">The value of <see cref="delegatedInstallers"/>.</param>
		public DelegatingEngineInstaller(FrozenDictionary<EngineType, IEngineInstaller> delegatedInstallers)
		{
			this.delegatedInstallers = delegatedInstallers ?? throw new ArgumentNullException(nameof(delegatedInstallers));
		}

		/// <inheritdoc />
		public Task CleanCache(CancellationToken cancellationToken)
			=> Task.WhenAll(delegatedInstallers.Values.Select(installer => installer.CleanCache(cancellationToken)));

		/// <inheritdoc />
		public ValueTask<IEngineInstallation> CreateInstallation(EngineVersion version, string path, Task installationTask, CancellationToken cancellationToken)
			=> DelegateCall(version, installer => installer.CreateInstallation(version, path, installationTask, cancellationToken));

		/// <inheritdoc />
		public ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken)
			=> DelegateCall(version, installer => installer.DownloadVersion(version, jobProgressReporter, cancellationToken));

		/// <inheritdoc />
		public ValueTask Install(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken)
			=> DelegateCall(version, installer => installer.Install(version, path, deploymentPipelineProcesses, cancellationToken));

		/// <inheritdoc />
		public ValueTask TrustDmbPath(EngineVersion version, string fullDmbPath, CancellationToken cancellationToken)
			=> DelegateCall(version, installer => installer.TrustDmbPath(version, fullDmbPath, cancellationToken));

		/// <inheritdoc />
		public ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken)
			=> DelegateCall(version, installer => installer.UpgradeInstallation(version, path, cancellationToken));

		/// <summary>
		/// Delegate a given <paramref name="call"/> to its appropriate <see cref="IEngineInstaller"/>.
		/// </summary>
		/// <typeparam name="TReturn">The return <see cref="Type"/> of the call.</typeparam>
		/// <param name="version">The <see cref="EngineVersion"/> used to perform delegate selection.</param>
		/// <param name="call">The <see cref="Func{T, TResult}"/> that will be called with the correct <see cref="IEngineInstaller"/> based on <paramref name="version"/>.</param>
		/// <returns>The <typeparamref name="TReturn"/> value of the delegated call.</returns>
		TReturn DelegateCall<TReturn>(EngineVersion version, Func<IEngineInstaller, TReturn> call)
		{
			ArgumentNullException.ThrowIfNull(version);
			return call(delegatedInstallers[version.Engine!.Value]);
		}
	}
}
