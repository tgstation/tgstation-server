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

using Microsoft.Extensions.Logging;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Common;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	/// <remarks>This <see langword="class"/> is a HACK but it works. Try not to break it if you wish to change it. Remember, this code doesn't get updated with the rest of the server.</remarks>
	sealed class Watchdog : IWatchdog
	{
		/// <summary>
		/// The <see cref="ISignalChecker"/> for the <see cref="Watchdog"/>.
		/// </summary>
		readonly ISignalChecker signalChecker;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Watchdog"/>.
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="Watchdog"/> class.
		/// </summary>
		/// <param name="signalChecker">The value of <see cref="signalChecker"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public Watchdog(ISignalChecker signalChecker, ILogger<Watchdog> logger)
		{
			this.signalChecker = signalChecker ?? throw new ArgumentNullException(nameof(signalChecker));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
#pragma warning disable CA1502 // TODO: Decomplexify
#pragma warning disable CA1506
		public async Task<bool> RunAsync(bool runConfigure, string[] args, CancellationToken cancellationToken)
		{
			logger.LogInformation("Host watchdog starting...");
			int currentProcessId;
			using (var currentProc = Process.GetCurrentProcess())
				currentProcessId = currentProc.Id;

			logger.LogDebug("PID: {pid}", currentProcessId);
			string updateDirectory = null;
			try
			{
				var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				var dotnetPath = GetDotnetPath(isWindows);
				if (dotnetPath == default)
				{
					logger.LogCritical("Unable to locate dotnet executable in PATH! Please ensure the .NET Core runtime is installed and is in your PATH!");
					return false;
				}

				logger.LogInformation("Detected dotnet executable at {dotnetPath}", dotnetPath);

				var executingAssembly = Assembly.GetExecutingAssembly();
				var rootLocation = Path.GetDirectoryName(executingAssembly.Location);

				var assemblyStoragePath = Path.Combine(rootLocation, "lib"); // always always next to watchdog

				var defaultAssemblyPath = Path.GetFullPath(Path.Combine(assemblyStoragePath, "Default"));

				if (Debugger.IsAttached)
				{
					// VS special tactics
					// just copy the shit where it belongs
					if (Directory.Exists(assemblyStoragePath))
						Directory.Delete(assemblyStoragePath, true);
					Directory.CreateDirectory(defaultAssemblyPath);

					var sourcePath = "../../../../Tgstation.Server.Host/bin/Debug/net6.0";
					foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
						Directory.CreateDirectory(dirPath.Replace(sourcePath, defaultAssemblyPath, StringComparison.Ordinal));

					foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
						File.Copy(newPath, newPath.Replace(sourcePath, defaultAssemblyPath, StringComparison.Ordinal), true);

					const string AppSettingsYaml = "appsettings.yml";
					var rootYaml = Path.Combine(rootLocation, AppSettingsYaml);
					File.Delete(rootYaml);
					File.Move(Path.Combine(defaultAssemblyPath, AppSettingsYaml), rootYaml);
				}
				else
					Directory.CreateDirectory(assemblyStoragePath);

				var assemblyName = String.Join(".", nameof(Tgstation), nameof(Server), nameof(Host), "dll");
				var assemblyPath = Path.Combine(defaultAssemblyPath, assemblyName);

				if (assemblyPath.Contains('"', StringComparison.Ordinal))
				{
					logger.LogCritical("Running from paths with \"'s in the name is not supported!");
					return false;
				}

				if (!File.Exists(assemblyPath))
				{
					logger.LogCritical("Unable to locate host assembly!");
					return false;
				}

				var watchdogVersion = executingAssembly.GetName().Version.Semver().ToString();

				while (!cancellationToken.IsCancellationRequested)
					using (logger.BeginScope("Host invocation"))
					{
						updateDirectory = Path.GetFullPath(Path.Combine(assemblyStoragePath, Guid.NewGuid().ToString()));
						logger.LogInformation("Update path set to {updateDirectory}", updateDirectory);
						using (var process = new Process())
						{
							process.StartInfo.FileName = dotnetPath;
							process.StartInfo.WorkingDirectory = rootLocation;  // for appsettings

							var arguments = new List<string>
							{
								$"\"{assemblyPath}\"",
								$"\"{updateDirectory}\"",
								$"\"{watchdogVersion}\"",
							};

							if (args.Any(x => x.Equals("--attach-host-debugger", StringComparison.OrdinalIgnoreCase)))
								arguments.Add("--attach-debugger");

							if (runConfigure)
							{
								logger.LogInformation("Running configuration check and wizard...");
								arguments.Add("--General:SetupWizardMode=Only");
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

							var killedHostProcess = false;
							try
							{
								var processTask = tcs.Task;
								(int, Task) StartProcess(string additionalArg)
								{
									if (additionalArg != null)
										process.StartInfo.Arguments += $" {additionalArg}";

									logger.LogInformation("Launching host with arguments: {arguments}", process.StartInfo.Arguments);

									process.Start();
									return (process.Id, processTask);
								}

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
									catch (ObjectDisposedException ex)
									{
										// race conditions
										logger.LogWarning(ex, "Error triggering timeout!");
									}
								}))
								{
									using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

									var checkerTask = signalChecker.CheckSignals(StartProcess, cts.Token);
									try
									{
										await processTask;
									}
									finally
									{
										cts.Cancel();
										await checkerTask;
									}
								}
							}
							catch (InvalidOperationException ex)
							{
								logger.LogWarning(ex, "Error triggering timeout!");
							}
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
								catch (InvalidOperationException ex2)
								{
									logger.LogWarning(ex2, "Error killing host process!");
								}

								try
								{
									if (File.Exists(updateDirectory))
										File.Delete(updateDirectory);
								}
								catch (Exception ex2)
								{
									logger.LogWarning(ex2, "Error deleting comms file!");
								}

								logger.LogInformation("Host exited!");
							}

							if (runConfigure)
							{
								logger.LogInformation("Exiting due to configure intent...");
								return true;
							}

							switch ((HostExitCode)process.ExitCode)
							{
								case HostExitCode.CompleteExecution:
									return true;
								case HostExitCode.RestartRequested:
									if (!cancellationToken.IsCancellationRequested)
										logger.LogInformation("Watchdog will restart host..."); // just a restart
									else
										logger.LogWarning("Host requested restart but watchdog shutdown is in progress!");
									break;
								case HostExitCode.Error:
									// update path is now an exception document
									logger.LogCritical("Host crashed, propagating exception dump...");

									var data = "(NOT PRESENT)";
									if (File.Exists(updateDirectory))
										data = File.ReadAllText(updateDirectory);

									try
									{
										File.Delete(updateDirectory);
									}
									catch (Exception e)
									{
										logger.LogWarning(e, "Unable to delete exception dump file at {updateDirectory}!", updateDirectory);
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
										logger.LogWarning(e, "Error deleting old server at {tempPath}!", tempPath);
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
				return false;
			}
			finally
			{
				logger.LogInformation("Host watchdog exiting...");
			}

			return true;
		}
#pragma warning restore CA1502
#pragma warning restore CA1506

		/// <summary>
		/// Gets the path to the dotnet executable.
		/// </summary>
		/// <param name="isWindows">If the current system is a Windows OS.</param>
		/// <returns>The path to the dotnet executable.</returns>
		string GetDotnetPath(bool isWindows)
		{
			var enviromentPath = Environment.GetEnvironmentVariable("PATH");
			var paths = enviromentPath.Split(';');

			var exeName = "dotnet";
			IEnumerable<string> enumerator;
			if (isWindows)
			{
				exeName += ".exe";
				enumerator = new List<string>(paths)
				{
					"C:/Program Files/dotnet",
					"C:/Program Files (x86)/dotnet",
				};
			}
			else
				enumerator = paths
					.Select(x => x.Split(':'))
					.SelectMany(x => x)
					.Concat(new List<string>(2)
					{
						"/usr/bin",
						"/usr/share/bin",
						"/usr/local/share/dotnet",
					});

			enumerator = enumerator.Select(x => Path.Combine(x, exeName));

			return enumerator
				.Where(potentialDotnetPath =>
				{
					logger.LogTrace("Checking for dotnet at {potentialDotnetPath}", potentialDotnetPath);
					return File.Exists(potentialDotnetPath);
				})
				.FirstOrDefault();
		}
	}
}
