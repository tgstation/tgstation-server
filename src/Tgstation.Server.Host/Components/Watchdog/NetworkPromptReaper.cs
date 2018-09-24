using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class NetworkPromptReaper : IHostedService, INetworkPromptReaper, IDisposable
	{
		/// <summary>
		/// Number of times to send the button click message. Should be at least 2 or it may fail to focus the window
		/// </summary>
		const int SendMessageCount = 5;
		
		/// <summary>
		/// Check for prompts each time this amount of milliseconds pass
		/// </summary>
		const int RecheckDelayMs = 250;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="NetworkPromptReaper"/>
		/// </summary>
		readonly ILogger<NetworkPromptReaper> logger;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="NetworkPromptReaper"/>
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The list of <see cref="IProcess"/>s registered 
		/// </summary>
		readonly List<IProcess> registeredProcesses;

		/// <summary>
		/// The <see cref="Task"/> representing the lifetime of the <see cref="NetworkPromptReaper"/>
		/// </summary>
		Task runTask;

		static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
		{
			var gcChildhandlesList = GCHandle.FromIntPtr(lParam);

			if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
				return false;

			var childHandles = (List <IntPtr>)gcChildhandlesList.Target;
			childHandles.Add(hWnd);

			return true;
		}

		static List<IntPtr> GetAllChildHandles(IntPtr main)
		{
			var childHandles = new List<IntPtr>();

			var gcChildhandlesList = GCHandle.Alloc(childHandles);
			var pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

			try
			{
				NativeMethods.EnumWindowProc childProc = new NativeMethods.EnumWindowProc(EnumWindow);
				NativeMethods.EnumChildWindows(main, childProc, pointerChildHandlesList);
			}
			finally
			{
				gcChildhandlesList.Free();
			}

			return childHandles;
		}

		/// <summary>
		/// Construct a <see cref="NetworkPromptReaper"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public NetworkPromptReaper(ILogger<NetworkPromptReaper> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			registeredProcesses = new List<IProcess>();
			cancellationTokenSource = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public void Dispose() => cancellationTokenSource.Dispose();

		async Task Run(CancellationToken cancellationToken)
		{
			logger.LogDebug("Starting network prompt reaper...");
			try
			{
				while(!cancellationToken.IsCancellationRequested)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(RecheckDelayMs), cancellationToken).ConfigureAwait(false);

					IntPtr window;
					int processId;
					lock (registeredProcesses)
					{
						if (registeredProcesses.Count == 0)
							continue;

						window = NativeMethods.FindWindow(null, "Network Accessibility");
						if (window == IntPtr.Zero)
							continue;

						//found a bitch
						var threadId = NativeMethods.GetWindowThreadProcessId(window, out processId);
						if (!registeredProcesses.Any(x => x.Id == processId))
							//not our bitch
							continue;
					}
					logger.LogTrace("Identified \"Network Accessibility\" window in owned process {0}", processId);

					var found = false;
					foreach(var I in GetAllChildHandles(window))
					{
						const int MaxLength = 10;
						var stringBuilder = new StringBuilder(MaxLength + 1);

						if(NativeMethods.GetWindowText(I, stringBuilder, MaxLength) == 0)
						{
							logger.LogWarning("Error calling GetWindowText! Exception: {0}", new Win32Exception(Marshal.GetLastWin32Error()));
							continue;
						}

						var windowText = stringBuilder.ToString();
						if (windowText == "Yes")
						{
							//smash_button_meme.jpg
							logger.LogTrace("Sending \"Yes\" button clicks...");
							for (var J = 0; J < SendMessageCount; ++J)
							{
								const int BM_CLICK = 0x00F5;
								var result = NativeMethods.SendMessage(I, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
							}
							found = true;
							break;
						}
					}

					if (!found)
						logger.LogDebug("Unable to find \"Yes\" button for \"Network Accessibility\" window in owned process {0}!", processId);
				}
			}
			finally
			{
				logger.LogDebug("Exiting network prompt reaper...");
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			runTask = Run(cancellationTokenSource.Token);
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogTrace("Stopping network prompt reaper...");
			cancellationTokenSource.Cancel();
			await runTask.ConfigureAwait(false);
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

			process.Lifetime.ContinueWith(x =>
			{
				logger.LogTrace("Unregistering process {0}...", process.Id);
				lock (registeredProcesses)
					registeredProcesses.Remove(process);
			}, TaskScheduler.Current);
		}
	}
}
