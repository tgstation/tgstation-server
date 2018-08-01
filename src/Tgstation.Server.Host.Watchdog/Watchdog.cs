using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// Construct a <see cref="Watchdog"/>
		/// </summary>
		/// <param name="activeLibraryDeleter">The value of <see cref="activeLibraryDeleter"/></param>
		/// <param name="isolatedAssemblyLoader">The value of <see cref="isolatedAssemblyLoader"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Watchdog(ILogger<Watchdog> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task RunAsync(string[] args, CancellationToken cancellationToken)
		{
			logger.LogInformation("Host watchdog starting...");

			var enviromentPath = Environment.GetEnvironmentVariable("PATH");
			var paths = enviromentPath.Split(';');

			var exeName = "dotnet";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				exeName += ".exe";

			var dotnetPath = paths.Select(x => Path.Combine(x, exeName))
							   .Where(x => File.Exists(x))
							   .FirstOrDefault();

			if(dotnetPath == default)
			{
				logger.LogCritical("Unable to locate dotnet executable in PATH! Please ensure the .NET Core runtime is installed and is in your PATH!");
				return;
			}
			logger.LogInformation("Detected dotnet executable at {0}", dotnetPath);

			var rootLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

#if DEBUG
			rootLocation = Path.GetFullPath("../../../../Tgstation.Server.Host.Watchdog/bin/Debug/netstandard2.0");
#endif

			var assemblyStoragePath = Path.Combine(rootLocation, "lib");	//always always next to watchdog

			var defaultAssemblyPath = Path.GetFullPath(Path.Combine(assemblyStoragePath, "Default"));
			var assemblyName = String.Join(".", nameof(Tgstation), nameof(Server), nameof(Host), "dll");
			var assemblyPath = Path.Combine(defaultAssemblyPath, assemblyName);

			if (assemblyPath.Contains("\""))
			{
				logger.LogCritical("Running from paths with \"'s in the name is not supported!");
				return;
			}

			if (!File.Exists(assemblyPath))
			{
				logger.LogCritical("Unable to locate host assembly!");
				return;
			}

			string updateDirectory = null;
			try
			{
				while (!cancellationToken.IsCancellationRequested)
					using (logger.BeginScope("Host invocation"))
					{
						updateDirectory = Path.GetFullPath(Path.Combine(assemblyStoragePath, Guid.NewGuid().ToString()));
						using (var process = new Process())
						{
							process.StartInfo.FileName = dotnetPath;
							process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;  //for appsettings

							var arguments = new List<string>
							{
								'"' + assemblyPath + '"',
								updateDirectory
							};
							arguments.AddRange(args);

							process.StartInfo.Arguments = String.Join(" ", arguments);

							process.StartInfo.UseShellExecute = false;  //runs in the same console

							var tcs = new TaskCompletionSource<object>();
							process.Exited += (a, b) =>
							{
								tcs.TrySetResult(null);
							};
							process.EnableRaisingEvents = true;
							
							logger.LogInformation("Launching host...");

							var iShotTheSheriff = false;
							try
							{
								process.Start();
								using (cancellationToken.Register(() =>
								{
									if (!Directory.Exists(updateDirectory))
									{
										logger.LogInformation("Cancellation requested! Writing shutdown lock file...");
										File.WriteAllBytes(updateDirectory, Array.Empty<byte>());
									}
									else
										logger.LogWarning("Cancellation requested while update directory exists!");

									logger.LogInformation("Will force close host process if it doesn't exit in 10 seconds...");

									Thread.Sleep(TimeSpan.FromSeconds(10)); //things get weird if we use tasks or other stuff
									tcs.TrySetResult(null);
								}))
									await tcs.Task.ConfigureAwait(false);
							}
							catch (InvalidOperationException) { }
							finally
							{
								try
								{
									if (!process.HasExited)
									{
										iShotTheSheriff = true;
										process.Kill();
										process.WaitForExit();
									}
								}
								catch (InvalidOperationException) { }
								logger.LogInformation("Host exited!");
							}

							switch (process.ExitCode)
							{
								case 0:
									//just a restart
									logger.LogInformation("Watchdog will restart host...");
									break;
								case 1:
									//update path is now an exception document
									logger.LogCritical("Host crashed, propagating exception dump...");
									var data = File.ReadAllText(updateDirectory);
									try
									{
										File.Delete(updateDirectory);
									}
									catch (Exception e)
									{
										logger.LogWarning("Unable to delete exception dump file at {0}! Exception: {1}", updateDirectory, e);
									}
									throw new Exception(String.Format(CultureInfo.InvariantCulture, "Host propagated exception: {0}", data));
								default:
									if (iShotTheSheriff)
									{
										logger.LogWarning("Watchdog forced to kill host process!");
										cancellationToken.ThrowIfCancellationRequested();
									}
									throw new Exception(String.Format(CultureInfo.InvariantCulture, "Host crashed with exit code {0}!", process.ExitCode));
							}
						}

						if (Directory.Exists(updateDirectory))
						{
							var tempPath = Path.Combine(assemblyStoragePath, Guid.NewGuid().ToString());
							Directory.Move(defaultAssemblyPath, tempPath);
							try
							{
								Directory.Move(updateDirectory, defaultAssemblyPath);
								logger.LogInformation("Server update complete, deleting old server...");
								try
								{
									Directory.Delete(tempPath, true);
								}
								catch (Exception e)
								{
									logger.LogWarning("Error deleting old server at {0}! Exception: {1}", tempPath, e);
								}
							}
							catch (Exception e)
							{
								logger.LogError("Error moving updated server directory, attempting revert! Exception: {0}", e);
								Directory.Delete(defaultAssemblyPath, true);
								Directory.Move(tempPath, defaultAssemblyPath);
								logger.LogInformation("Revert successful!");
							}
						}
					}
			}
			catch (OperationCanceledException)
			{
				logger.LogDebug("Exiting due to cancellation...");
				if (!Directory.Exists(updateDirectory))
					File.Delete(updateDirectory);
				else
					Directory.Delete(updateDirectory, true);
			}
			catch (Exception e)
			{
				logger.LogCritical("Watchdog error! Exception: {0}", e);
			}
			logger.LogInformation("Host watchdog exiting...");
		}
	}
}
