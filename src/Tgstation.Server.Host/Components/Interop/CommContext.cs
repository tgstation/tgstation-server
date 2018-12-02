using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <inheritdoc />
	sealed class CommContext : ICommContext
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="CommContext"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="CommContext"/>
		/// </summary>
		readonly ILogger<CommContext> logger;

		/// <summary>
		/// The <see cref="FileSystemWatcher"/> for the <see cref="CommContext"/>
		/// </summary>
		readonly FileSystemWatcher fileSystemWatcher;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="CommContext"/>
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="CancellationToken"/> for the <see cref="CommContext"/>
		/// </summary>
		readonly CancellationToken cancellationToken;

		/// <summary>
		/// The <see cref="ICommHandler"/> for the <see cref="CommContext"/>
		/// </summary>
		ICommHandler handler;

		/// <summary>
		/// If the <see cref="CommContext"/> has been disposed
		/// </summary>
		bool disposed;

		/// <summary>
		/// Construct an <see cref="CommContext"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="directory">The path to watch</param>
		/// <param name="filter">The filter to watch for</param>
		public CommContext(IIOManager ioManager, ILogger<CommContext> logger, string directory, string filter)
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
			disposed = false;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			fileSystemWatcher.Dispose();
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		/// <summary>
		/// Runs when the <see cref="fileSystemWatcher"/> triggers
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="FileSystemEventArgs"/></param>
		async void HandleWrite(object sender, FileSystemEventArgs e) // this is what async void was made for
		{
			try
			{
				var fileBytes = await ioManager.ReadAllBytes(e.FullPath, cancellationToken).ConfigureAwait(false);
				var file = Encoding.UTF8.GetString(fileBytes);

				logger.LogTrace("Read interop command json: {0}", file);

				CommCommand command;
				try
				{
					command = new CommCommand
					{
						Parameters = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, object>>(file),
						RawJson = file
					};
				}
				catch (JsonException ex)
				{
					// file not fully written yet
					logger.LogDebug("Suppressing json convert exception for command file write: {0}", ex);
					return;
				}

				await (handler?.HandleInterop(command, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
			}
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				logger.LogDebug("Exception while trying to handle command json write: {0}", ex);
			}
		}

		/// <inheritdoc />
		public void RegisterHandler(ICommHandler handler)
		{
			if (this.handler != null)
				throw new InvalidOperationException("RegisterHandler already called!");
			this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}
	}
}