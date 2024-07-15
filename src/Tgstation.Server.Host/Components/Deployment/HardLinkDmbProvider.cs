using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses hard links.
	/// </summary>
	[UnsupportedOSPlatform("windows")]
	sealed class HardLinkDmbProvider : SwappableDmbProvider
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="mirroringTask"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task"/> representing the base provider mirroring operation.
		/// </summary>
		readonly Task<string?> mirroringTask;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="HardLinkDmbProvider"/>.
		/// </summary>
		readonly ILogger logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="HardLinkDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The <see cref="IDmbProvider"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="linkFactory">The <see cref="IFilesystemLinkFactory"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfiguration">The <see cref="GeneralConfiguration"/> for the <see cref="HardLinkDmbProvider"/>.</param>
		/// <param name="securityLevel">The launch <see cref="DreamDaemonSecurity"/> level.</param>
		public HardLinkDmbProvider(
			IDmbProvider baseProvider,
			IIOManager ioManager,
			IFilesystemLinkFactory linkFactory,
			ILogger logger,
			GeneralConfiguration generalConfiguration,
			DreamDaemonSecurity securityLevel)
			: base(
				 baseProvider,
				 ioManager,
				 linkFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			cancellationTokenSource = new CancellationTokenSource();
			try
			{
				mirroringTask = MirrorSourceDirectory(generalConfiguration.GetCopyDirectoryTaskThrottle(), securityLevel, cancellationTokenSource.Token);
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
			var mirroredDir = await mirroringTask;
			if (mirroredDir != null && !Swapped)
			{
				logger.LogDebug("Cancelled mirroring task, we must cleanup!");

				// We shouldn't be doing long running I/O ops because this could be under an HTTP request (DELETE /api/DreamDaemon)
				// dirty shit to follow:
				async void AsyncCleanup()
				{
					try
					{
						await IOManager.DeleteDirectory(mirroredDir, CancellationToken.None); // DCT: None available
						logger.LogTrace("Completed async cleanup of unused mirror directory: {mirroredDir}", mirroredDir);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error cleaning up mirrored directory {mirroredDir}!", mirroredDir);
					}
				}

				AsyncCleanup();
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
		protected override async ValueTask DoSwap(CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin DoSwap, mirroring task complete: {complete}...", mirroringTask.IsCompleted);
			var mirroredDir = await mirroringTask.WaitAsync(cancellationToken);
			if (mirroredDir == null)
			{
				// huh, how?
				cancellationToken.ThrowIfCancellationRequested();
				throw new InvalidOperationException("mirroringTask was cancelled without us being cancelled?");
			}

			var goAheadTcs = new TaskCompletionSource();

			// I feel dirty...
			async void DisposeOfOldDirectory()
			{
				var directoryMoved = false;
				var disposeGuid = Guid.NewGuid();
				var disposePath = disposeGuid.ToString();
				logger.LogTrace("Moving Live directory to {path} for deletion...", disposeGuid);
				try
				{
					await IOManager.MoveDirectory(LiveGameDirectory, disposePath, cancellationToken);
					directoryMoved = true;
					goAheadTcs.SetResult();
					logger.LogTrace("Deleting old Live directory {path}...", disposePath);
					await IOManager.DeleteDirectory(disposePath, CancellationToken.None); // DCT: We're detached at this point
					logger.LogTrace("Completed async cleanup of old Live directory: {disposePath}", disposePath);
				}
				catch (DirectoryNotFoundException ex)
				{
					logger.LogDebug(ex, "Live directory appears to not exist");
					if (!directoryMoved)
						goAheadTcs.SetResult();
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to delete hard linked directory: {disposePath}", disposePath);
					if (!directoryMoved)
						goAheadTcs.SetException(ex);
				}
			}

			DisposeOfOldDirectory();
			await goAheadTcs.Task;
			logger.LogTrace("Moving mirror directory {path} to Live...", mirroredDir);
			await IOManager.MoveDirectory(mirroredDir, LiveGameDirectory, cancellationToken);
			logger.LogTrace("Swap complete!");
		}

		/// <summary>
		/// Mirror the <see cref="Models.CompileJob"/>.
		/// </summary>
		/// <param name="taskThrottle">The optional maximum number of simultaneous tasks allowed to execute.</param>
		/// <param name="securityLevel">The launch <see cref="DreamDaemonSecurity"/> level.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the full path to the mirrored directory.</returns>
		async Task<string?> MirrorSourceDirectory(int? taskThrottle, DreamDaemonSecurity securityLevel, CancellationToken cancellationToken)
		{
			if (taskThrottle.HasValue && taskThrottle < 1)
				throw new ArgumentOutOfRangeException(nameof(taskThrottle), taskThrottle, "taskThrottle must be at least 1!");

			string? dest = null;
			try
			{
				var stopwatch = Stopwatch.StartNew();
				var mirrorGuid = Guid.NewGuid();

				logger.LogDebug("Starting to mirror {sourceDir} as hard links to {mirrorGuid}...", CompileJob.DirectoryName, mirrorGuid);

				var src = IOManager.ResolvePath(CompileJob.DirectoryName!.Value.ToString());
				dest = IOManager.ResolvePath(mirrorGuid.ToString());

				using var semaphore = taskThrottle.HasValue ? new SemaphoreSlim(taskThrottle.Value) : null;
				await Task.WhenAll(MirrorDirectoryImpl(
					src,
					dest,
					semaphore,
					securityLevel,
					cancellationToken));
				stopwatch.Stop();

				logger.LogDebug(
					"Finished mirror of {sourceDir} to {mirrorGuid} in {seconds}s...",
					CompileJob.DirectoryName,
					mirrorGuid,
					stopwatch.Elapsed.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture));
			}
			catch (OperationCanceledException ex)
			{
				logger.LogDebug(ex, "Cancelled while mirroring");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Could not mirror!");

				if (dest != null)
					try
					{
						logger.LogDebug("Cleaning up mirror attempt: {dest}", dest);
						await IOManager.DeleteDirectory(dest, cancellationToken);
					}
					catch (OperationCanceledException ex2)
					{
						logger.LogDebug(ex2, "Errored cleanup cancellation edge case!");
						return dest;
					}

				return null;
			}

			return dest;
		}

		/// <summary>
		/// Recursively create tasks to create a hard link directory mirror of <paramref name="src"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="src">The source directory path.</param>
		/// <param name="dest">The destination directory path.</param>
		/// <param name="semaphore">Optional <see cref="SemaphoreSlim"/> used to limit degree of parallelism.</param>
		/// <param name="securityLevel">The launch <see cref="DreamDaemonSecurity"/> level.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IEnumerable{T}"/> of <see cref="Task"/>s representing the running operations. The first <see cref="Task"/> returned is always the necessary call to <see cref="IIOManager.CreateDirectory(string, CancellationToken)"/>.</returns>
		/// <remarks>I genuinely don't know how this will work with symlinked files. Waiting for the issue report I guess.</remarks>
		IEnumerable<Task> MirrorDirectoryImpl(string src, string dest, SemaphoreSlim? semaphore, DreamDaemonSecurity securityLevel, CancellationToken cancellationToken)
		{
			var dir = new DirectoryInfo(src);
			Task? subdirCreationTask = null;
			var dreamDaemonWillAcceptOutOfDirectorySymlinks = securityLevel == DreamDaemonSecurity.Trusted;
			foreach (var subDirectory in dir.EnumerateDirectories())
			{
				var mirroredName = Path.Combine(dest, subDirectory.Name);

				// check if we are a symbolic link
				if (subDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
					if (dreamDaemonWillAcceptOutOfDirectorySymlinks)
					{
						var target = subDirectory.ResolveLinkTarget(false)
							?? throw new InvalidOperationException($"\"{subDirectory.FullName}\" was incorrectly identified as a symlinked directory!");
						logger.LogDebug("Recreating directory {name} as symlink to {target}", subDirectory.Name, target);
						if (subdirCreationTask == null)
						{
							subdirCreationTask = IOManager.CreateDirectory(dest, cancellationToken);
							yield return subdirCreationTask;
						}

						async Task CopyLink()
						{
							await subdirCreationTask.WaitAsync(cancellationToken);
							using var lockContext = semaphore != null
								? await SemaphoreSlimContext.Lock(semaphore, cancellationToken)
								: null;
							await LinkFactory.CreateSymbolicLink(target.FullName, mirroredName, cancellationToken);
						}

						yield return CopyLink();
						continue;
					}
					else
						logger.LogDebug("Recreating symlinked directory {name} as hard links...", subDirectory.Name);

				var checkingSubdirCreationTask = true;
				foreach (var copyTask in MirrorDirectoryImpl(subDirectory.FullName, mirroredName, semaphore, securityLevel, cancellationToken))
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

					if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
					{
						// AHHHHHHHHHHHHH
						var target = fileInfo.ResolveLinkTarget(!dreamDaemonWillAcceptOutOfDirectorySymlinks)
							?? throw new InvalidOperationException($"\"{fileInfo.FullName}\" was incorrectly identified as a symlinked file!");

						if (dreamDaemonWillAcceptOutOfDirectorySymlinks)
						{
							logger.LogDebug("Recreating symlinked file {name} as symlink to {target}", fileInfo.Name, target.FullName);
							await LinkFactory.CreateSymbolicLink(target.FullName, destFile, cancellationToken);
						}
						else
						{
							logger.LogDebug("Recreating symlinked file {name} as hard link to {target}", fileInfo.Name, target.FullName);
							await LinkFactory.CreateHardLink(target.FullName, destFile, cancellationToken);
						}
					}
					else
						await LinkFactory.CreateHardLink(sourceFile, destFile, cancellationToken);
				}

				yield return LinkThisFile();
			}
		}
	}
}
