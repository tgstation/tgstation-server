using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BetterWin32Errors;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc cref="INetworkPromptReaper" />
	sealed class WindowsNetworkPromptReaper : BackgroundService, INetworkPromptReaper
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
				NativeMethods.EnumWindowProc childProc = new(EnumWindow);
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
		public void RegisterProcess(IProcess process)
		{
			ArgumentNullException.ThrowIfNull(process);

			lock (registeredProcesses)
			{
				if (registeredProcesses.Contains(process))
					throw new InvalidOperationException("This process has already been registered for network prompt reaping!");
				logger.LogTrace("Registering process {pid}...", process.Id);
				registeredProcesses.Add(process);
			}

			process.Lifetime.ContinueWith(
				x =>
				{
					logger.LogTrace("Unregistering process {pid}...", process.Id);
					lock (registeredProcesses)
						registeredProcesses.Remove(process);
				},
				TaskScheduler.Current);
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			logger.LogDebug("Starting network prompt reaper...");
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					await asyncDelayer.Delay(TimeSpan.FromMilliseconds(RecheckDelayMs), cancellationToken);

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

					logger.LogTrace("Identified \"Network Accessibility\" window in owned process {pid}", processId);

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
						logger.LogDebug("Unable to find \"Yes\" button for \"Network Accessibility\" window in owned process {pid}!", processId);
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Cancelled!");
			}
			finally
			{
				registeredProcesses.Clear();
				logger.LogTrace("Exiting network prompt reaper...");
			}
		}
	}
}
