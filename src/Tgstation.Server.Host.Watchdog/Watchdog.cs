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
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Watchdog(ILogger<Watchdog> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		string GetDotnetPath(bool isWindows)
		{
			var enviromentPath = Environment.GetEnvironmentVariable("PATH");
			var paths = enviromentPath.Split(';');

			var exeName = "dotnet";
			IEnumerable<string> enumerator;
			if (isWindows)
			{
				exeName += ".exe";
				enumerator = paths;
			}
			else
				enumerator = paths.Select(x => x.Split(':')).SelectMany(x => x);

			enumerator = enumerator.Select(x => Path.Combine(x, exeName));

			return enumerator
				.Where(x =>
				{
					logger.LogTrace("Checking for dotnet at {0}", x);
					return File.Exists(x);
				})
				.FirstOrDefault();
		}

		/// <inheritdoc />
		#pragma warning disable CA1502 // TODO: Decomplexify
		#pragma warning disable CA1506
		public async Task RunAsync(bool runConfigure, string[] args, CancellationToken cancellationToken)
		{
			logger.LogInformation("Host watchdog starting...");
			logger.LogDebug("PID: {0}", Process.GetCurrentProcess().Id);
			string updateDirectory = null;
			try
			{
				var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				var dotnetPath = GetDotnetPath(isWindows);
				if (dotnetPath == default)
				{
					logger.LogCritical("Unable to locate dotnet executable in PATH! Please ensure the .NET Core runtime is installed and is in your PATH!");
					return;
				}

				logger.LogInformation("Detected dotnet executable at {0}", dotnetPath);

				var executingAssembly = Assembly.GetExecutingAssembly();
				var rootLocation = Path.GetDirectoryName(executingAssembly.Location);

				var assemblyStoragePath = Path.Combine(rootLocation, "lib"); // always always next to watchdog

				var defaultAssemblyPath = Path.GetFullPath(Path.Combine(assemblyStoragePath, "Default"));

				if (Debugger.IsAttached)
				{
					// VS special tactics
					// just copy the shit where it belongs
					Directory.Delete(assemblyStoragePath, true);
					Directory.CreateDirectory(defaultAssemblyPath);

					var sourcePath = "../../../../Tgstation.Server.Host/bin/Debug/netcoreapp3.1";
					foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
						Directory.CreateDirectory(dirPath.Replace(sourcePath, defaultAssemblyPath));

					foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
						File.Copy(newPath, newPath.Replace(sourcePath, defaultAssemblyPath), true);

					const string AppSettingsYaml = "appsettings.yml";
					var rootYaml = Path.Combine(rootLocation, AppSettingsYaml);
					File.Delete(rootYaml);
					File.Move(Path.Combine(defaultAssemblyPath, AppSettingsYaml), rootYaml);
				}
				else
					Directory.CreateDirectory(assemblyStoragePath);

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

				string watchdogVersion = executingAssembly.GetName().Version.ToString();

				while (!cancellationToken.IsCancellationRequested)
					using (logger.BeginScope("Host invocation"))
					{
						updateDirectory = Path.GetFullPath(Path.Combine(assemblyStoragePath, Guid.NewGuid().ToString()));
						logger.LogInformation("Update path set to {0}", updateDirectory);
						using (var process = new Process())
						{
							process.StartInfo.FileName = dotnetPath;
							process.StartInfo.WorkingDirectory = rootLocation;  // for appsettings

							var arguments = new List<string>
							{
								$"\"{assemblyPath}\"",
								$"\"{updateDirectory}\"",
								$"\"{watchdogVersion}\""
							};

							if (args.Any(x => x.Equals("--attach-host-debugger", StringComparison.OrdinalIgnoreCase)))
								arguments.Add("--attach-debugger");

							if (runConfigure)
							{
								logger.LogInformation("Running configuration check and wizard if necessary...");
								arguments.Add("General:SetupWizardMode=Only");
							}

							arguments.AddRange(args);

							process.StartInfo.Arguments = String.Join(" ", arguments);

							process.StartInfo.UseShellExecute = false; // runs in the same console

							var tcs = new TaskCompletionSource<object>();
							process.Exited += (a, b) =>
							{
								tcs.TrySetResult(null);
							};
							process.EnableRaisingEvents = true;

							logger.LogInformation("Launching host...");

							var killedHostProcess = false;
							try
							{
								process.Start();

								using (var processCts = new CancellationTokenSource())
								using (processCts.Token.Register(() => tcs.TrySetResult(null)))
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

									try
									{
										processCts.CancelAfter(TimeSpan.FromSeconds(10));
									}
									catch (ObjectDisposedException) { } // race conditions
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
										killedHostProcess = true;
										process.Kill();
										process.WaitForExit();
									}
								}
								catch (InvalidOperationException) { }
								logger.LogInformation("Host exited!");
							}

							if (runConfigure)
							{
								logger.LogInformation("Exiting due to configuration check...");
								return;
							}

							switch (process.ExitCode)
							{
								case 0:
									return;
								case 1:
									if (!cancellationToken.IsCancellationRequested)
										logger.LogInformation("Watchdog will restart host..."); // just a restart
									else
										logger.LogWarning("Host requested restart but watchdog shutdown is in progress!");
									break;
								case 2:
									// update path is now an exception document
									logger.LogCritical("Host crashed, propagating exception dump...");
									var data = File.ReadAllText(updateDirectory);
									try
									{
										File.Delete(updateDirectory);
									}
									catch (Exception e)
									{
										logger.LogWarning(e, "Unable to delete exception dump file at {0}!", updateDirectory);
									}

#pragma warning disable CA2201 // Do not raise reserved exception types
									throw new Exception(String.Format(CultureInfo.InvariantCulture, "Host propagated exception: {0}", data));
#pragma warning restore CA2201 // Do not raise reserved exception types
								default:
									if (killedHostProcess)
									{
										logger.LogWarning("Watchdog forced to kill host process!");
										cancellationToken.ThrowIfCancellationRequested();
									}

#pragma warning disable CA2201 // Do not raise reserved exception types
									throw new Exception(String.Format(CultureInfo.InvariantCulture, "Host crashed with exit code {0}!", process.ExitCode));
#pragma warning restore CA2201 // Do not raise reserved exception types
							}
						}

						// HEY YOU
						// BE WARNED THAT IF YOU DEBUGGED THE HOST PROCESS THAT JUST LAUNCHED THE DEBUGGER WILL HOLD A LOCK ON THE DIRECTORY
						// THIS MEANS THE FIRST DIRECTORY.MOVE WILL THROW
						if (Directory.Exists(updateDirectory))
						{
							logger.LogInformation("Applying server update...");
							if (isWindows)
							{
								// windows dick sucking resource unlocking
								GC.Collect(Int32.MaxValue, GCCollectionMode.Default, true);
								await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
							}

							var tempPath = Path.Combine(assemblyStoragePath, Guid.NewGuid().ToString());
							try
							{
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
										logger.LogWarning(e, "Error deleting old server at {0}!", tempPath);
									}
								}
								catch (Exception e)
								{
									logger.LogError(e, "Error moving updated server directory, attempting revert!");
									Directory.Delete(defaultAssemblyPath, true);
									Directory.Move(tempPath, defaultAssemblyPath);
									logger.LogInformation("Revert successful!");
								}
							}
							catch (Exception e)
							{
								logger.LogWarning(e, "Failed to move out active host assembly!");
							}
						}
					}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogDebug(ex, "Exiting due to cancellation...");
				if (!Directory.Exists(updateDirectory))
					File.Delete(updateDirectory);
				else
					Directory.Delete(updateDirectory, true);
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, "Host watchdog error!");
			}
			finally
			{
				logger.LogInformation("Host watchdog exiting...");
			}
		}
		#pragma warning restore CA1502
		#pragma warning restore CA1506
	}
}
