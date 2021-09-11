using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BetterWin32Errors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	[SupportedOSPlatform("windows")]
	sealed class WindowsNetworkPromptReaper : IHostedService, INetworkPromptReaper, IAsyncDisposable
	{
		/// <summary>
		/// Number of times to send the button click message. Should be at least 2 or it may fail to focus the window.
		/// </summary>
		const int SendMessageCount = 5;

		/// <summary>
		/// Check for prompts each time this amount of milliseconds pass.
		/// </summary>
		const int RecheckDelayMs = 250;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="WindowsNetworkPromptReaper"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="WindowsNetworkPromptReaper"/>.
		/// </summary>
		readonly ILogger<WindowsNetworkPromptReaper> logger;

		/// <summary>
		/// The list of <see cref="IProcess"/>s registered.
		/// </summary>
		readonly List<IProcess> registeredProcesses;

		/// <summary>
		/// The <see cref="Task"/> representing the lifetime of the <see cref="WindowsNetworkPromptReaper"/>.
		/// </summary>
		CancellableTask? runTask;

		/// <summary>
		/// Callback for <see cref="NativeMethods.EnumChildWindows(IntPtr, NativeMethods.EnumWindowProc, IntPtr)"/>.
		/// </summary>
		/// <param name="hWnd">The window handle.</param>
		/// <param name="lParam">Unused.</param>
		/// <returns><see langword="true"/> if enumeration should continue, <see langword="false"/> otherwise.</returns>
		static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
		{
			var gcChildhandlesList = GCHandle.FromIntPtr(lParam);

			if (gcChildhandlesList.Target == null)
				return false;

			var childHandles = (List<IntPtr>)gcChildhandlesList.Target;
			childHandles.Add(hWnd);

			return true;
		}

		/// <summary>
		/// Get all the child windows handles of a given <paramref name="mainWindow"/>.
		/// </summary>
		/// <param name="mainWindow">The handle of the window to enumerate.</param>
		/// <returns>A <see cref="List{T}"/> of all the child handles of <paramref name="mainWindow"/>.</returns>
		static List<IntPtr> GetAllChildHandles(IntPtr mainWindow)
		{
			var childHandles = new List<IntPtr>();
			var gcChildhandlesList = GCHandle.Alloc(childHandles);
			try
			{
				var pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);
				NativeMethods.EnumWindowProc childProc = new NativeMethods.EnumWindowProc(EnumWindow);
				NativeMethods.EnumChildWindows(mainWindow, childProc, pointerChildHandlesList);
			}
			finally
			{
				gcChildhandlesList.Free();
			}

			return childHandles;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsNetworkPromptReaper"/> class.
		/// </summary>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public WindowsNetworkPromptReaper(IAsyncDelayer asyncDelayer, ILogger<WindowsNetworkPromptReaper> logger)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			registeredProcesses = new List<IProcess>();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			if (runTask != null)
			{
				await runTask.DisposeAsync().ConfigureAwait(false);
				runTask = null;
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if(runTask != null)
				throw new InvalidOperationException("WindowsNetworkPromptReaper already started!");

			runTask = new CancellableTask(token => Run(token));
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (runTask == null)
				throw new InvalidOperationException("WindowsNetworkPromptReaper not started!");

			logger.LogTrace("Stopping network prompt reaper...");
			runTask.Cancel();
			await DisposeAsync().ConfigureAwait(false);
			registeredProcesses.Clear();
		}

		/// <inheritdoc />
		public void RegisterProcess(IProcess process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

			lock (registeredProcesses)
			{
				if (registeredProcesses.Contains(process))
					throw new InvalidOperationException("This process has already been registered for network prompt reaping!");
				logger.LogTrace("Registering process {0}...", process.Id);
				registeredProcesses.Add(process);
			}

			process.Lifetime.ContinueWith(
				x =>
			{
				logger.LogTrace("Unregistering process {0}...", process.Id);
				lock (registeredProcesses)
					registeredProcesses.Remove(process);
			}, TaskScheduler.Current);
		}

		/// <summary>
		/// Main loop for the <see cref="WindowsNetworkPromptReaper"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task Run(CancellationToken cancellationToken)
		{
			logger.LogDebug("Starting network prompt reaper...");
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					await asyncDelayer.Delay(TimeSpan.FromMilliseconds(RecheckDelayMs), cancellationToken).ConfigureAwait(false);

					IntPtr window;
					int processId;
					lock (registeredProcesses)
					{
						if (registeredProcesses.Count == 0)
							continue;

						window = NativeMethods.FindWindow(null, "Network Accessibility");
						if (window == IntPtr.Zero)
							continue;

						// found a bitch
						var threadId = NativeMethods.GetWindowThreadProcessId(window, out processId);
						if (!registeredProcesses.Any(x => x.Id == processId))
							continue; // not our bitch
					}

					logger.LogTrace("Identified \"Network Accessibility\" window in owned process {0}", processId);

					var found = false;
					foreach (var childHandle in GetAllChildHandles(window))
					{
						const int MaxLength = 10;
						var stringBuilder = new StringBuilder(MaxLength + 1);

						if (NativeMethods.GetWindowText(childHandle, stringBuilder, MaxLength) == 0)
						{
							logger.LogWarning(new Win32Exception(), "Error calling GetWindowText!");
							continue;
						}

						var windowText = stringBuilder.ToString();
						if (windowText == "Yes")
						{
							// smash_button_meme.jpg
							logger.LogTrace("Sending \"Yes\" button clicks...");
							for (var iteration = 0; iteration < SendMessageCount; ++iteration)
							{
								const int BM_CLICK = 0x00F5;
								var result = NativeMethods.SendMessage(childHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
							}

							found = true;
							break;
						}
					}

					if (!found)
						logger.LogDebug("Unable to find \"Yes\" button for \"Network Accessibility\" window in owned process {0}!", processId);
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Cancelled!");
			}
			finally
			{
				logger.LogDebug("Exiting network prompt reaper...");
			}
		}
	}
}
