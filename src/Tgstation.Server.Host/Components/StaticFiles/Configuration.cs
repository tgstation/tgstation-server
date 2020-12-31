using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <inheritdoc />
	sealed class Configuration : IConfiguration
	{
		const string CodeModificationsSubdirectory = "CodeModifications";
		const string EventScriptsSubdirectory = "EventScripts";
		const string GameStaticFilesSubdirectory = "GameStaticFiles";

		/// <summary>
		/// Name of the ignore file in <see cref="GameStaticFilesSubdirectory"/>
		/// </summary>
		const string StaticIgnoreFile = ".tgsignore";

		const string CodeModificationsHeadFile = "HeadInclude.dm";
		const string CodeModificationsTailFile = "TailInclude.dm";

		static readonly IReadOnlyDictionary<EventType, string> EventTypeScriptFileNameMap = new Dictionary<EventType, string>(
			Enum.GetValues(typeof(EventType))
				.OfType<EventType>()
				.Select(
					eventType => new KeyValuePair<EventType, string>(
						eventType,
						typeof(EventType)
							.GetField(eventType.ToString())
							.GetCustomAttributes(false)
							.OfType<EventScriptAttribute>()
							.First()
							.ScriptName)));

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="Configuration"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ISynchronousIOManager"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ISynchronousIOManager synchronousIOManager;

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for <see cref="Configuration"/>
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for <see cref="Configuration"/>
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for <see cref="Configuration"/>
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for <see cref="Configuration"/>.
		/// </summary>>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ILogger<Configuration> logger;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for <see cref="Configuration"/>. Also used as a <see langword="lock"/> <see cref="object"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> that is triggered when <see cref="IDisposable.Dispose"/> is called.
		/// </summary>
		readonly CancellationTokenSource disposeCts;

		/// <summary>
		/// The culmination of all upload file transfer callbacks.
		/// </summary>
		Task uploadTasks;

		/// <summary>
		/// Construct <see cref="Configuration"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="synchronousIOManager">The value of <see cref="synchronousIOManager"/></param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/></param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/></param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Configuration(
			IIOManager ioManager,
			ISynchronousIOManager synchronousIOManager,
			ISymlinkFactory symlinkFactory,
			IProcessExecutor processExecutor,
			IPostWriteHandler postWriteHandler,
			IPlatformIdentifier platformIdentifier,
			IFileTransferTicketProvider fileTransferService,
			ILogger<Configuration> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
			this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			semaphore = new SemaphoreSlim(1);
			disposeCts = new CancellationTokenSource();
			uploadTasks = Task.CompletedTask;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			semaphore.Dispose();
			disposeCts.Cancel();
			disposeCts.Dispose();
		}

		/// <summary>
		/// Get the proper path to <see cref="StaticIgnoreFile"/>
		/// </summary>
		/// <returns>The <see cref="ioManager"/> relative path to <see cref="StaticIgnoreFile"/></returns>
		string StaticIgnorePath() => ioManager.ConcatPath(GameStaticFilesSubdirectory, StaticIgnoreFile);

		/// <summary>
		/// Ensures standard configuration directories exist
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task EnsureDirectories(CancellationToken cancellationToken)
		{
			async Task ValidateStaticFolder()
			{
				await ioManager.CreateDirectory(GameStaticFilesSubdirectory, cancellationToken).ConfigureAwait(false);
				var staticIgnorePath = StaticIgnorePath();
				if(!await ioManager.FileExists(staticIgnorePath, cancellationToken).ConfigureAwait(false))
					await ioManager.WriteAllBytes(staticIgnorePath, Array.Empty<byte>(), cancellationToken).ConfigureAwait(false);
			}

			await Task.WhenAll(ioManager.CreateDirectory(CodeModificationsSubdirectory, cancellationToken), ioManager.CreateDirectory(EventScriptsSubdirectory, cancellationToken), ValidateStaticFolder()).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ServerSideModifications> CopyDMFilesTo(string dmeFile, string destination, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				await EnsureDirectories(cancellationToken).ConfigureAwait(false);

				// just assume no other fs race conditions here
				var dmeExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, dmeFile), cancellationToken);
				var headFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsHeadFile), cancellationToken);
				var tailFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsTailFile), cancellationToken);
				var copyTask = ioManager.CopyDirectory(CodeModificationsSubdirectory, destination, null, cancellationToken);

				await Task.WhenAll(dmeExistsTask, headFileExistsTask, tailFileExistsTask, copyTask).ConfigureAwait(false);

				if (!dmeExistsTask.Result && !headFileExistsTask.Result && !tailFileExistsTask.Result)
					return null;

				if (dmeExistsTask.Result)
					return new ServerSideModifications(null, null, true);

				if (!headFileExistsTask.Result && !tailFileExistsTask.Result)
					return null;

				static string IncludeLine(string filePath) => String.Format(CultureInfo.InvariantCulture, "#include \"{0}\"", filePath);

				return new ServerSideModifications(headFileExistsTask.Result ? IncludeLine(CodeModificationsHeadFile) : null, tailFileExistsTask.Result ? IncludeLine(CodeModificationsTailFile) : null, false);
			}
		}

		string ValidateConfigRelativePath(string configurationRelativePath)
		{
			var nullOrEmptyCheck = String.IsNullOrEmpty(configurationRelativePath);
			if (nullOrEmptyCheck)
				configurationRelativePath = DefaultIOManager.CurrentDirectory;
			if (configurationRelativePath[0] == Path.DirectorySeparatorChar || configurationRelativePath[0] == Path.AltDirectorySeparatorChar)
				configurationRelativePath = DefaultIOManager.CurrentDirectory + configurationRelativePath;
			var resolved = ioManager.ResolvePath(configurationRelativePath);
			var local = !nullOrEmptyCheck ? ioManager.ResolvePath() : null;
			if (!nullOrEmptyCheck && resolved.Length < local.Length) // .. fuccbois
				throw new InvalidOperationException("Attempted to access file outside of configuration manager!");
			return resolved;
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<ConfigurationFile>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			if (configurationRelativePath == null)
				configurationRelativePath = "/";

			List<ConfigurationFile> result = new List<ConfigurationFile>();

			void ListImpl()
			{
				var enumerator = synchronousIOManager.GetDirectories(path, cancellationToken);
				try
				{
					result.AddRange(enumerator.Select(x => new ConfigurationFile
					{
						IsDirectory = true,
						Path = ioManager.ConcatPath(configurationRelativePath, x),
					}).OrderBy(file => file.Path));
				}
				catch (IOException e)
				{
					logger.LogDebug(e, "IOException while writing {0}!", path);
					result = null;
					return;
				}

				enumerator = synchronousIOManager.GetFiles(path, cancellationToken);
				result.AddRange(enumerator.Select(x => new ConfigurationFile
				{
					IsDirectory = false,
					Path = ioManager.ConcatPath(configurationRelativePath, x),
				}).OrderBy(file => file.Path));
			}

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				if (systemIdentity == null)
					ListImpl();
				else
					await systemIdentity.RunImpersonated(ListImpl, cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <inheritdoc />
		public async Task<ConfigurationFile> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			ConfigurationFile result = null;

			void ReadImpl()
			{
				lock (semaphore)
					try
					{
						string GetFileSha()
						{
							var content = synchronousIOManager.ReadFile(path);
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
							using var sha1 = new SHA1Managed();
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
							return String.Join(String.Empty, sha1.ComputeHash(content).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
						}

						var originalSha = GetFileSha();

						var disposeToken = disposeCts.Token;
						var fileTicket = fileTransferService.CreateDownload(
							new FileDownloadProvider(
								() =>
								{
									if (disposeToken.IsCancellationRequested)
										return ErrorCode.InstanceOffline;

									var newSha = GetFileSha();
									if (newSha != originalSha)
										return ErrorCode.ConfigurationFileUpdated;

									return null;
								},
								async cancellationToken =>
								{
									FileStream result = null;
									void GetFileStream()
									{
										result = ioManager.GetFileStream(path, false);
									}

									using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
										if (systemIdentity == null)
											await Task.Factory.StartNew(GetFileStream, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current).ConfigureAwait(false);
										else
											await systemIdentity.RunImpersonated(GetFileStream, cancellationToken).ConfigureAwait(false);

									return result;
								},
								path,
								false));

						result = new ConfigurationFile
						{
							FileTicket = fileTicket.FileTicket,
							IsDirectory = false,
							LastReadHash = originalSha,
							AccessDenied = false,
							Path = configurationRelativePath
						};
					}
					catch (UnauthorizedAccessException)
					{
						// this happens on windows, dunno about linux
						bool isDirectory;
						try
						{
							isDirectory = synchronousIOManager.IsDirectory(path);
						}
						catch
						{
							isDirectory = false;
						}

						result = new ConfigurationFile
						{
							Path = configurationRelativePath
						};
						if (!isDirectory)
							result.AccessDenied = true;
						else
							result.IsDirectory = true;
					}
			}

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				if (systemIdentity == null)
					await Task.Factory.StartNew(ReadImpl, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current).ConfigureAwait(false);
				else
					await systemIdentity.RunImpersonated(ReadImpl, cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <inheritdoc />
		public async Task SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken)
		{
			async Task<IReadOnlyList<string>> GetIgnoreFiles()
			{
				var ignoreFileBytes = await ioManager.ReadAllBytes(StaticIgnorePath(), cancellationToken).ConfigureAwait(false);
				var ignoreFileText = Encoding.UTF8.GetString(ignoreFileBytes);

				var results = new List<string> { StaticIgnoreFile };

				// we don't want to lose trailing whitespace on linux
				using (var reader = new StringReader(ignoreFileText))
				{
					cancellationToken.ThrowIfCancellationRequested();
					var line = await reader.ReadLineAsync().ConfigureAwait(false);
					if (!String.IsNullOrEmpty(line))
						results.Add(line);
				}

				return results;
			}

			IReadOnlyList<string> ignoreFiles;

			async Task SymlinkBase(bool files)
			{
				Task<IReadOnlyList<string>> task;
				if (files)
					task = ioManager.GetFiles(GameStaticFilesSubdirectory, cancellationToken);
				else
					task = ioManager.GetDirectories(GameStaticFilesSubdirectory, cancellationToken);
				var entries = await task.ConfigureAwait(false);

				await Task.WhenAll(entries.Select(async x =>
				{
					var fileName = ioManager.GetFileName(x);

					// need to normalize
					bool ignored;
					if (platformIdentifier.IsWindows)
						ignored = ignoreFiles.Any(y => fileName.ToUpperInvariant() == y.ToUpperInvariant());
					else
						ignored = ignoreFiles.Any(y => fileName == y);

					if (ignored)
					{
						logger.LogTrace("Ignoring static file {0}...", fileName);
						return;
					}

					var destPath = ioManager.ConcatPath(destination, fileName);
					logger.LogTrace("Symlinking {0} to {1}...", x, destPath);
					var fileExistsTask = ioManager.FileExists(destPath, cancellationToken);
					if (await ioManager.DirectoryExists(destPath, cancellationToken).ConfigureAwait(false))
						await ioManager.DeleteDirectory(destPath, cancellationToken).ConfigureAwait(false);
					var fileExists = await fileExistsTask.ConfigureAwait(false);
					if (fileExists)
						await ioManager.DeleteFile(destPath, cancellationToken).ConfigureAwait(false);
					await symlinkFactory.CreateSymbolicLink(ioManager.ResolvePath(x), ioManager.ResolvePath(destPath), cancellationToken).ConfigureAwait(false);
				})).ConfigureAwait(false);
			}

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				await EnsureDirectories(cancellationToken).ConfigureAwait(false);
				ignoreFiles = await GetIgnoreFiles().ConfigureAwait(false);
				await Task.WhenAll(SymlinkBase(true), SymlinkBase(false)).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task<ConfigurationFile> Write(string configurationRelativePath, ISystemIdentity systemIdentity, string previousHash, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			ConfigurationFile result = null;

			void WriteImpl()
			{
				lock (semaphore)
					try
					{
						var fileTicket = fileTransferService.CreateUpload(true);
						var uploadCancellationToken = disposeCts.Token;
						async Task UploadHandler()
						{
							using (fileTicket)
							{
								var fileHash = previousHash;
								using var uploadStream = await fileTicket.GetResult(uploadCancellationToken).ConfigureAwait(false);
								bool success = false;
								void WriteCallback()
								{
									success = synchronousIOManager.WriteFileChecked(path, uploadStream, ref fileHash, cancellationToken);
								}

								using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
									if (systemIdentity == null)
										await Task.Factory.StartNew(WriteCallback, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current).ConfigureAwait(false);
									else
										await systemIdentity.RunImpersonated(WriteCallback, cancellationToken).ConfigureAwait(false);

								if (!success)
									fileTicket.SetErrorMessage(new ErrorMessage(ErrorCode.ConfigurationFileUpdated)
									{
										AdditionalData = fileHash
									});
								else if(uploadStream.Length > 0)
									postWriteHandler.HandleWrite(path);
							}
						}

						result = new ConfigurationFile
						{
							FileTicket = fileTicket.Ticket.FileTicket,
							LastReadHash = previousHash,
							IsDirectory = false,
							AccessDenied = false,
							Path = configurationRelativePath
						};

						lock (disposeCts)
							uploadTasks = Task.WhenAll(uploadTasks, UploadHandler());
					}
					catch (UnauthorizedAccessException)
					{
						// this happens on windows, dunno about linux
						bool isDirectory;
						try
						{
							isDirectory = synchronousIOManager.IsDirectory(path);
						}
						catch
						{
							isDirectory = false;
						}

						result = new ConfigurationFile
						{
							Path = configurationRelativePath
						};
						if (!isDirectory)
							result.AccessDenied = true;
						else
							result.IsDirectory = true;
					}
			}

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				if (systemIdentity == null)
					await Task.Factory.StartNew(WriteImpl, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current).ConfigureAwait(false);
				else
					await systemIdentity.RunImpersonated(WriteImpl, cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> CreateDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			bool? result = null;
			void DoCreate() => result = synchronousIOManager.CreateDirectory(path, cancellationToken);

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				if (systemIdentity == null)
					await Task.Factory.StartNew(DoCreate, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current).ConfigureAwait(false);
				else
					await systemIdentity.RunImpersonated(DoCreate, cancellationToken).ConfigureAwait(false);

			return result.Value;
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => EnsureDirectories(cancellationToken);

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => EnsureDirectories(cancellationToken);

		/// <inheritdoc />
		public async Task HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);

			if (!EventTypeScriptFileNameMap.TryGetValue(eventType, out var scriptName))
				return;

			// always execute in serial
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				var files = await ioManager.GetFilesWithExtension(EventScriptsSubdirectory, platformIdentifier.ScriptFileExtension, false, cancellationToken).ConfigureAwait(false);
				var resolvedScriptsDir = ioManager.ResolvePath(EventScriptsSubdirectory);

				var scriptFiles = files
					.Select(x => ioManager.GetFileName(x))
					.Where(x => x.StartsWith(scriptName, StringComparison.Ordinal))
					.ToList();

				if (!scriptFiles.Any())
				{
					logger.LogTrace("No event scripts starting with \"{0}\" detected", scriptName);
					return;
				}

				foreach (var I in scriptFiles)
				{
					logger.LogTrace("Running event script {0}...", I);
					using (var script = processExecutor.LaunchProcess(
						ioManager.ConcatPath(resolvedScriptsDir, I),
						resolvedScriptsDir,
						String.Join(
							' ',
							parameters.Select(arg =>
							{
								if (!arg.Contains(' ', StringComparison.Ordinal))
									return arg;

								arg = arg.Replace("\"", "\\\"", StringComparison.Ordinal);

								return $"\"{arg}\"";
							})),
						true,
						true,
						true))
					using (cancellationToken.Register(() => script.Terminate()))
					{
						var exitCode = await script.Lifetime.ConfigureAwait(false);
						cancellationToken.ThrowIfCancellationRequested();
						var scriptOutput = await script.GetCombinedOutput(cancellationToken).ConfigureAwait(false);
						if (exitCode != 0)
							throw new JobException($"Script {I} exited with code {exitCode}:{Environment.NewLine}{scriptOutput}");
						else
							logger.LogDebug("Script output:{0}{1}", Environment.NewLine, scriptOutput);
					}
				}
			}
		}

		/// <inheritdoc />
		public async Task<bool> DeleteDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken).ConfigureAwait(false);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			var result = false;
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				void CheckDeleteImpl() => result = synchronousIOManager.DeleteDirectory(path);

				if (systemIdentity != null)
					await systemIdentity.RunImpersonated(CheckDeleteImpl, cancellationToken).ConfigureAwait(false);
				else
					CheckDeleteImpl();
			}

			return result;
		}
	}
}
