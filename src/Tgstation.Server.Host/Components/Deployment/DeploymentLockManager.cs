using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Manages locks on a given <see cref="IDmbProvider"/>.
	/// </summary>
	sealed class DeploymentLockManager : IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="Models.CompileJob"/> represented by the <see cref="DeploymentLockManager"/>.
		/// </summary>
		public CompileJob CompileJob => dmbProvider.CompileJob;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DeploymentLockManager"/>.
		/// </summary>
		readonly ILogger logger;

		/// <summary>
		/// The <see cref="IDmbProvider"/> the <see cref="DeploymentLockManager"/> is managing.
		/// </summary>
		readonly IDmbProvider dmbProvider;

		/// <summary>
		/// The <see cref="DmbLock"/>s on the <see cref="dmbProvider"/>.
		/// </summary>
		readonly HashSet<DmbLock> locks;

		/// <summary>
		/// The first lock acquired by the <see cref="DeploymentLockManager"/>.
		/// </summary>
		readonly DmbLock firstLock;

		/// <summary>
		/// Create a <see cref="DeploymentLockManager"/>.
		/// </summary>
		/// <param name="dmbProvider">The value of <see cref="dmbProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="initialLockReason">The reason for the first lock.</param>
		/// <param name="firstLock">The <see cref="IDmbProvider"/> that represents the first lock.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A new <see cref="DeploymentLockManager"/>.</returns>
		public static DeploymentLockManager Create(IDmbProvider dmbProvider, ILogger logger, string initialLockReason, out IDmbProvider firstLock, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default)
		{
			var manager = new DeploymentLockManager(dmbProvider, logger, initialLockReason, callerFile!, callerLine);
			firstLock = manager.firstLock;
			return manager;
		}

		/// <summary>
		/// Generates a verbose description of a given <paramref name="dmbLock"/>.
		/// </summary>
		/// <param name="dmbLock">The <see cref="DmbLock"/> to get a description of.</param>
		/// <returns>A verbose description of <paramref name="dmbLock"/>.</returns>
		static string GetFullLockDescriptor(DmbLock dmbLock) => $"{dmbLock.LockID} {dmbLock.Descriptor} (Created at {dmbLock.LockTime}){(dmbLock.KeptAlive ? " (RELEASED)" : String.Empty)}";

		/// <summary>
		/// Initializes a new instance of the <see cref="DeploymentLockManager"/> class.
		/// </summary>
		/// <param name="dmbProvider">The value of <see cref="dmbProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="initialLockReason">The reason for the first lock.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A new <see cref="DeploymentLockManager"/>.</returns>
		DeploymentLockManager(IDmbProvider dmbProvider, ILogger logger, string initialLockReason, string callerFile, int callerLine)
		{
			this.dmbProvider = dmbProvider ?? throw new ArgumentNullException(nameof(dmbProvider));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			logger.LogTrace("Initializing lock manager for compile job {id}", dmbProvider.CompileJob.Id);
			locks = new HashSet<DmbLock>();
			firstLock = CreateLock(initialLockReason, callerFile, callerLine);
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync()
			=> firstLock.DisposeAsync();

		/// <summary>
		/// Add a lock to the managed <see cref="IDmbProvider"/>.
		/// </summary>
		/// <param name="reason">The reason for the lock.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A <see cref="IDmbProvider"/> whose lifetime represents the lock.</returns>
		public IDmbProvider AddLock(string reason, [CallerFilePath] string? callerFile = null, [CallerLineNumber]int callerLine = default)
		{
			ArgumentNullException.ThrowIfNull(reason);
			lock (locks)
			{
				if (locks.Count == 0)
					throw new InvalidOperationException($"No locks exist on the DmbProvider for CompileJob {dmbProvider.CompileJob.Id}!");

				return CreateLock(reason, callerFile!, callerLine);
			}
		}

		/// <summary>
		/// Add lock stats to a given <paramref name="stringBuilder"/>.
		/// </summary>
		/// <param name="stringBuilder">The <see cref="StringBuilder"/> to append to.</param>
		public void LogLockStats(StringBuilder stringBuilder)
		{
			ArgumentNullException.ThrowIfNull(stringBuilder);

			stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Compile Job #{CompileJob.Id}: {CompileJob.DirectoryName}");
			lock (locks)
				foreach (var dmbLock in locks)
					stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"\t-{GetFullLockDescriptor(dmbLock)}");
		}

		/// <summary>
		/// Creates a <see cref="DmbLock"/> and adds it to <see cref="locks"/>.
		/// </summary>
		/// <param name="reason">The reason for the lock.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A new <see cref="DmbLock"/>.</returns>
		/// <remarks>Requires exclusive write access to <see cref="locks"/> be held by the caller.</remarks>
		DmbLock CreateLock(string reason, string callerFile, int callerLine)
		{
			DmbLock? newLock = null;
			string? descriptor = null;
			ValueTask LockCleanupAction()
			{
				ValueTask disposeTask = ValueTask.CompletedTask;
				lock (locks)
				{
					logger.LogTrace("Removing .dmb Lock: {descriptor}", descriptor);

					if (locks.Remove(newLock!))
						logger.LogTrace("Lock was removed from list successfully");
					else
						logger.LogError("A .dmb lock was disposed more than once: {descriptor}", descriptor);

					if (locks.Count == 0)
						disposeTask = dmbProvider.DisposeAsync();
					else if (newLock == firstLock)
						logger.LogDebug("First lock on CompileJob #{compileJobId} removed, it must cleanup {remaining} remaining locks to be cleaned", CompileJob.Id, locks.Count);
				}

				return disposeTask;
			}

			newLock = new DmbLock(LockCleanupAction, dmbProvider, $"{callerFile}#{callerLine}: {reason}");
			locks.Add(newLock);

			descriptor = GetFullLockDescriptor(newLock!);
			logger.LogTrace("Created .dmb Lock: {descriptor}", descriptor);

			return newLock;
		}
	}
}
