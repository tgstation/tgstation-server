using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses hard links.
	/// </summary>
	sealed class HardLinkDmbProvider : SwappableDmbProvider
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="mirroringTask"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task"/> representing the base provider mirroring operation.
		/// </summary>
		readonly Task<string> mirroringTask;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="HardLinkDmbProvider"/>.
		/// </summary>
		readonly ILogger logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="HardLinkDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The <see cref="IDmbProvider"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="symlinkFactory">The <see cref="ISymlinkFactory"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfiguration">The <see cref="GeneralConfiguration"/> for the <see cref="HardLinkDmbProvider"/>.</param>
		public HardLinkDmbProvider(
			IDmbProvider baseProvider,
			IIOManager ioManager,
			ISymlinkFactory symlinkFactory,
			ILogger logger,
			GeneralConfiguration generalConfiguration)
			: base(
				 baseProvider,
				 ioManager,
				 symlinkFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			cancellationTokenSource = new CancellationTokenSource();
			try
			{
				mirroringTask = MirrorSourceDirectory(generalConfiguration.GetCopyDirectoryTaskThrottle(), cancellationTokenSource.Token);
			}
			catch
			{
				cancellationTokenSource.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			try
			{
				await mirroringTask;
			}
			catch (OperationCanceledException ex)
			{
				logger.LogDebug(ex, "Mirroring task cancelled!");
			}

			await base.DisposeAsync();
		}

		/// <inheritdoc />
		public override Task FinishActivationPreparation(CancellationToken cancellationToken)
		{
			if (!mirroringTask.IsCompleted)
				logger.LogTrace("Waiting for mirroring to complete...");

			return mirroringTask.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		protected override async Task DoSwap(CancellationToken cancellationToken)
		{
			var mirroredDir = await mirroringTask.WaitAsync(cancellationToken);
			var goAheadTcs = new TaskCompletionSource();

			// I feel dirty...
			async void DisposeOfOldDirectory()
			{
				var directoryMoved = false;
				var disposePath = Guid.NewGuid().ToString();
				try
				{
					await IOManager.MoveDirectory(LiveGameDirectory, disposePath, cancellationToken);
					directoryMoved = true;
					goAheadTcs.SetResult();
					await IOManager.DeleteDirectory(disposePath, CancellationToken.None); // DCT: We're detached at this point
				}
				catch (Exception ex)
				{
					if (directoryMoved)
						logger.LogWarning(ex, "Failed to delete hard linked directory: {disposePath}", disposePath);
					else
					{
						logger.LogDebug(ex, "Live directory appears to not exist");
						goAheadTcs.SetResult();
					}
				}
			}

			DisposeOfOldDirectory();
			await goAheadTcs.Task;
			await IOManager.MoveDirectory(mirroredDir, LiveGameDirectory, cancellationToken);
		}

		/// <summary>
		/// Mirror the <see cref="Models.CompileJob"/>.
		/// </summary>
		/// <param name="taskThrottle">The optional maximum number of simultaneous tasks allowed to execute.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the full path to the mirrored directory.</returns>
		async Task<string> MirrorSourceDirectory(int? taskThrottle, CancellationToken cancellationToken)
		{
			var stopwatch = Stopwatch.StartNew();
			var mirrorGuid = Guid.NewGuid();
			logger.LogDebug("Starting to mirror {sourceDir} as hard links to {mirrorGuid}...", CompileJob.DirectoryName, mirrorGuid);
			if (taskThrottle.HasValue && taskThrottle < 1)
				throw new ArgumentOutOfRangeException(nameof(taskThrottle), taskThrottle, "taskThrottle must be at least 1!");

			var src = IOManager.ResolvePath(CompileJob.DirectoryName.ToString());
			var dest = IOManager.ResolvePath(mirrorGuid.ToString());

			using var semaphore = taskThrottle.HasValue ? new SemaphoreSlim(taskThrottle.Value) : null;
			await Task.WhenAll(MirrorDirectoryImpl(src, dest, semaphore, cancellationToken));
			stopwatch.Stop();

			logger.LogDebug(
				"Finished mirror of {sourceDir} to {mirrorGuid} in {seconds}s...",
				CompileJob.DirectoryName,
				mirrorGuid,
				stopwatch.Elapsed.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture));

			return dest;
		}

		/// <summary>
		/// Recursively create tasks to create a hard link directory mirror of <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="src">The source directory path.</param>
		/// <param name="dest">The destination directory path.</param>
		/// <param name="semaphore">Optional <see cref="SemaphoreSlim"/> used to limit degree of parallelism.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of <see cref="Task"/>s representing the running operations. The first <see cref="Task"/> returned is always the necessary call to <see cref="IIOManager.CreateDirectory(string, CancellationToken)"/>.</returns>
		/// <remarks>I genuinely don't know how this will work with symlinked files. Waiting for the issue report I guess.</remarks>
		IEnumerable<Task> MirrorDirectoryImpl(string src, string dest, SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			var dir = new DirectoryInfo(src);
			Task subdirCreationTask = null;
			foreach (var subDirectory in dir.EnumerateDirectories())
			{
				// check if we are a symbolic link
				if (!subDirectory.Attributes.HasFlag(FileAttributes.Directory) || subDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
				{
					logger.LogTrace("Skipping symlink to {subdir}", subDirectory.Name);
					continue;
				}

				var checkingSubdirCreationTask = true;
				foreach (var copyTask in MirrorDirectoryImpl(subDirectory.FullName, Path.Combine(dest, subDirectory.Name), semaphore, cancellationToken))
				{
					if (subdirCreationTask == null)
					{
						subdirCreationTask = copyTask;
						yield return subdirCreationTask;
					}
					else if (!checkingSubdirCreationTask)
						yield return copyTask;

					checkingSubdirCreationTask = false;
				}
			}

			foreach (var fileInfo in dir.EnumerateFiles())
			{
				if (subdirCreationTask == null)
				{
					subdirCreationTask = IOManager.CreateDirectory(dest, cancellationToken);
					yield return subdirCreationTask;
				}

				var sourceFile = fileInfo.FullName;
				var destFile = IOManager.ConcatPath(dest, fileInfo.Name);

				async Task LinkThisFile()
				{
					await subdirCreationTask.WaitAsync(cancellationToken);
					using var lockContext = semaphore != null
						? await SemaphoreSlimContext.Lock(semaphore, cancellationToken)
						: null;
					await SymlinkFactory.CreateHardLink(sourceFile, destFile, cancellationToken);
				}

				yield return LinkThisFile();
			}
		}
	}
}
