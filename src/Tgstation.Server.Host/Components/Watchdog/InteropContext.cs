using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class InteropContext : IInteropContext
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InteropContext"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InteropContext"/>
		/// </summary>
		readonly ILogger<InteropContext> logger;

		/// <summary>
		/// The <see cref="FileSystemWatcher"/> for the <see cref="InteropContext"/>
		/// </summary>
		readonly FileSystemWatcher fileSystemWatcher;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="InteropContext"/>
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="CancellationToken"/> for the <see cref="InteropContext"/>
		/// </summary>
		readonly CancellationToken cancellationToken;

		/// <summary>
		/// The <see cref="IInteropHandler"/> for the <see cref="InteropContext"/>
		/// </summary>
		IInteropHandler handler;

		/// <summary>
		/// Construct an <see cref="InteropContext"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="directory">The path to watch</param>
		/// <param name="filter">The filter to watch for</param>
		public InteropContext(IIOManager ioManager, ILogger<InteropContext> logger, string directory, string filter)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			directory = ioManager.ResolvePath(directory) ?? throw new ArgumentNullException(nameof(directory));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			fileSystemWatcher = new FileSystemWatcher(directory, filter)
			{
				EnableRaisingEvents = true,
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastWrite
			};

			fileSystemWatcher.Created += HandleWrite;
			fileSystemWatcher.Changed += HandleWrite;

			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			fileSystemWatcher.Dispose();
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		/// <summary>
		/// Runs when the <see cref="fileSystemWatcher"/> triggers
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="FileSystemEventArgs"/></param>
		async void HandleWrite(object sender, FileSystemEventArgs e)	//this is what async void was made for
		{
			logger.LogTrace("FileSystemWatcher triggered...");
			try
			{
				var fileBytes = await ioManager.ReadAllBytes(e.FullPath, cancellationToken).ConfigureAwait(false);
				var file = Encoding.UTF8.GetString(fileBytes);

				InteropCommand command;
				try
				{
					command = JsonConvert.DeserializeObject<InteropCommand>(file);
				}
				catch (JsonSerializationException ex)
				{  
					//file not fully written yet
					logger.LogTrace("Suppressing json convert exception: {0}", ex);
					return;
				} 

				await (handler?.HandleInterop(command, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogDebug("Exception while trying to read command json: {0}", ex);
			}
		}

		/// <inheritdoc />
		public void RegisterHandler(IInteropHandler handler)
		{
			if (this.handler != null)
				throw new InvalidOperationException("RegisterHandler already called!");
			this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}
	}
}