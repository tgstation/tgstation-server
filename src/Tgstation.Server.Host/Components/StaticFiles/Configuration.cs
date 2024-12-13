using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <inheritdoc />
	sealed class Configuration : IConfiguration
	{
		/// <summary>
		/// The CodeModifications directory name.
		/// </summary>
		const string CodeModificationsSubdirectory = "CodeModifications";

		/// <summary>
		/// The EventScripts directory name.
		/// </summary>
		const string EventScriptsSubdirectory = "EventScripts";

		/// <summary>
		/// The GameStaticFiles directory name.
		/// </summary>
		const string GameStaticFilesSubdirectory = "GameStaticFiles";

		/// <summary>
		/// Name of the ignore file in <see cref="GameStaticFilesSubdirectory"/>.
		/// </summary>
		const string StaticIgnoreFile = ".tgsignore";

		/// <summary>
		/// The HeadInclude.dm filename.
		/// </summary>
		const string CodeModificationsHeadFile = "HeadInclude.dm";

		/// <summary>
		/// The TailInclude.dm filename.
		/// </summary>
		const string CodeModificationsTailFile = "TailInclude.dm";

		/// <summary>
		/// Default contents of <see cref="CodeModificationsHeadFile"/>.
		/// </summary>
		static readonly string DefaultHeadInclude = @$"// TGS AUTO GENERATED HeadInclude.dm{Environment.NewLine}// This file will be included BEFORE all code in your .dme IF a replacement .dme does not exist in this directory{Environment.NewLine}// Please note that changes need to be made available if you are hosting an AGPL licensed codebase{Environment.NewLine}// The presence file in its default state does not constitute a code change that needs to be published by licensing standards{Environment.NewLine}";

		/// <summary>
		/// Default contents of <see cref="CodeModificationsHeadFile"/>.
		/// </summary>
		static readonly string DefaultTailInclude = @$"// TGS AUTO GENERATED TailInclude.dm{Environment.NewLine}// This file will be included AFTER all code in your .dme IF a replacement .dme does not exist in this directory{Environment.NewLine}// Please note that changes need to be made available if you are hosting an AGPL licensed codebase{Environment.NewLine}// The presence file in its default state does not constitute a code change that needs to be published by licensing standards{Environment.NewLine}";

		/// <summary>
		/// Map of <see cref="EventType"/>s to the filename of the event scripts they trigger.
		/// </summary>
		public static IReadOnlyDictionary<EventType, string[]> EventTypeScriptFileNameMap { get; } = new Dictionary<EventType, string[]>(
			Enum.GetValues(typeof(EventType))
				.Cast<EventType>()
				.Select(
					eventType => new KeyValuePair<EventType, string[]>(
						eventType,
						typeof(EventType)
							.GetField(eventType.ToString())!
							.GetCustomAttributes(false)
							.OfType<EventScriptAttribute>()
							.First()
							.ScriptNames)));

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ISynchronousIOManager"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly ISynchronousIOManager synchronousIOManager;

		/// <summary>
		/// The <see cref="IFilesystemLinkFactory"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly IFilesystemLinkFactory linkFactory;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for <see cref="Configuration"/>.
		/// </summary>>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly ILogger<Configuration> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for <see cref="Configuration"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for <see cref="Configuration"/>. Also used as a <see langword="lock"/> <see cref="object"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> that is triggered when <see cref="StopAsync(CancellationToken)"/> is called.
		/// </summary>
		readonly CancellationTokenSource stoppingCts;

		/// <summary>
		/// The culmination of all upload file transfer callbacks.
		/// </summary>
		Task uploadTasks;

		/// <summary>
		/// Initializes a new instance of the <see cref="Configuration"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="synchronousIOManager">The value of <see cref="synchronousIOManager"/>.</param>
		/// <param name="linkFactory">The value of <see cref="linkFactory"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfiguration">The value of <see cref="generalConfiguration"/>.</param>
		/// <param name="sessionConfiguration">The value of <see cref="sessionConfiguration"/>.</param>
		public Configuration(
			IIOManager ioManager,
			ISynchronousIOManager synchronousIOManager,
			IFilesystemLinkFactory linkFactory,
			IProcessExecutor processExecutor,
			IPostWriteHandler postWriteHandler,
			IPlatformIdentifier platformIdentifier,
			IFileTransferTicketProvider fileTransferService,
			ILogger<Configuration> logger,
			GeneralConfiguration generalConfiguration,
			SessionConfiguration sessionConfiguration)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
			this.linkFactory = linkFactory ?? throw new ArgumentNullException(nameof(linkFactory));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.generalConfiguration = generalConfiguration ?? throw new ArgumentNullException(nameof(generalConfiguration));
			this.sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));

			semaphore = new SemaphoreSlim(1, 1);
			stoppingCts = new CancellationTokenSource();
			uploadTasks = Task.CompletedTask;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			semaphore.Dispose();
			stoppingCts.Dispose();
		}

		/// <inheritdoc />
		public async ValueTask<ServerSideModifications?> CopyDMFilesTo(string dmeFile, string destination, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken, logger))
			{
				var ensureDirectoriesTask = EnsureDirectories(cancellationToken);

				// just assume no other fs race conditions here
				var dmeExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, dmeFile), cancellationToken);
				var headFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsHeadFile), cancellationToken);
				var tailFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsTailFile), cancellationToken);

				await ensureDirectoriesTask;
				var copyTask = ioManager.CopyDirectory(
					null,
					null,
					CodeModificationsSubdirectory,
					destination,
					generalConfiguration.GetCopyDirectoryTaskThrottle(),
					cancellationToken);

				await Task.WhenAll(dmeExistsTask, headFileExistsTask, tailFileExistsTask, copyTask.AsTask());

				if (!dmeExistsTask.Result && !headFileExistsTask.Result && !tailFileExistsTask.Result)
					return null;

				if (dmeExistsTask.Result)
					return new ServerSideModifications(null, null, true);

				if (!headFileExistsTask.Result && !tailFileExistsTask.Result)
					return null;

				static string IncludeLine(string filePath) => String.Format(CultureInfo.InvariantCulture, "#include \"{0}\"", filePath);

				return new ServerSideModifications(
					headFileExistsTask.Result
						? IncludeLine(CodeModificationsHeadFile)
						: null,
					tailFileExistsTask.Result
						? IncludeLine(CodeModificationsTailFile)
						: null,
					false);
			}
		}

		/// <inheritdoc />
		public async ValueTask<IOrderedQueryable<ConfigurationFileResponse>?> ListDirectory(string? configurationRelativePath, ISystemIdentity? systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			configurationRelativePath ??= "/";

			var result = new List<ConfigurationFileResponse>();

			void ListImpl()
			{
				var enumerator = synchronousIOManager.GetDirectories(path, cancellationToken);
				result.AddRange(enumerator.Select(x => new ConfigurationFileResponse
				{
					IsDirectory = true,
					Path = ioManager.ConcatPath(configurationRelativePath, x),
				}));

				enumerator = synchronousIOManager.GetFiles(path, cancellationToken);
				result.AddRange(enumerator.Select(x => new ConfigurationFileResponse
				{
					IsDirectory = false,
					Path = ioManager.ConcatPath(configurationRelativePath, x),
				}));
			}

			using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
			{
				if (!locked)
				{
					logger.LogDebug("Contention when attempting to enumerate directory!");
					return null;
				}

				if (systemIdentity == null)
					ListImpl();
				else
					await systemIdentity.RunImpersonated(ListImpl, cancellationToken);
			}

			return result
				.AsQueryable()
				.OrderBy(configFile => !configFile.IsDirectory)
				.ThenBy(configFile => configFile.Path);
		}

		/// <inheritdoc />
		public async ValueTask<ConfigurationFileResponse?> Read(string configurationRelativePath, ISystemIdentity? systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			ConfigurationFileResponse? result = null;

			void ReadImpl()
			{
				try
				{
					string GetFileSha()
					{
						var content = synchronousIOManager.ReadFile(path);
						return String.Join(String.Empty, SHA1.HashData(content).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
					}

					var originalSha = GetFileSha();

					var disposeToken = stoppingCts.Token;
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
								FileStream? result = null;
								void GetFileStream()
								{
									result = ioManager.GetFileStream(path, false);
								}

								if (systemIdentity == null)
									await Task.Factory.StartNew(GetFileStream, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
								else
									await systemIdentity.RunImpersonated(GetFileStream, cancellationToken);

								return result!;
							},
							path,
							false));

					result = new ConfigurationFileResponse
					{
						FileTicket = fileTicket.FileTicket,
						IsDirectory = false,
						LastReadHash = originalSha,
						AccessDenied = false,
						Path = configurationRelativePath,
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
					catch (Exception ex)
					{
						logger.LogDebug(ex, "IsDirectory exception!");
						isDirectory = false;
					}

					result = new ConfigurationFileResponse
					{
						Path = configurationRelativePath,
					};
					if (!isDirectory)
						result.AccessDenied = true;

					result.IsDirectory = isDirectory;
				}
			}

			using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
			{
				if (!locked)
				{
					logger.LogDebug("Contention when attempting to read file!");
					return null;
				}

				if (systemIdentity == null)
					await Task.Factory.StartNew(ReadImpl, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
				else
					await systemIdentity.RunImpersonated(ReadImpl, cancellationToken);
			}

			return result;
		}

		/// <inheritdoc />
		public async ValueTask SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken)
		{
			List<string> ignoreFiles;

			async ValueTask SymlinkBase(bool files)
			{
				Task<IReadOnlyList<string>> task;
				if (files)
					task = ioManager.GetFiles(GameStaticFilesSubdirectory, cancellationToken);
				else
					task = ioManager.GetDirectories(GameStaticFilesSubdirectory, cancellationToken);
				var entries = await task;

				await ValueTaskExtensions.WhenAll(entries.Select<string, ValueTask>(async file =>
				{
					var fileName = ioManager.GetFileName(file);

					// need to normalize
					var fileComparison = platformIdentifier.IsWindows
						? StringComparison.OrdinalIgnoreCase
						: StringComparison.Ordinal;
					var ignored = ignoreFiles.Any(y => fileName.Equals(y, fileComparison));
					if (ignored)
					{
						logger.LogTrace("Ignoring static file {fileName}...", fileName);
						return;
					}

					var destPath = ioManager.ConcatPath(destination, fileName);
					logger.LogTrace("Symlinking {filePath} to {destPath}...", file, destPath);
					var fileExistsTask = ioManager.FileExists(destPath, cancellationToken);
					if (await ioManager.DirectoryExists(destPath, cancellationToken))
						await ioManager.DeleteDirectory(destPath, cancellationToken);
					var fileExists = await fileExistsTask;
					if (fileExists)
						await ioManager.DeleteFile(destPath, cancellationToken);
					await linkFactory.CreateSymbolicLink(ioManager.ResolvePath(file), ioManager.ResolvePath(destPath), cancellationToken);
				}));
			}

			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken, logger))
			{
				await EnsureDirectories(cancellationToken);
				var ignoreFileBytes = await ioManager.ReadAllBytes(StaticIgnorePath(), cancellationToken);
				var ignoreFileText = Encoding.UTF8.GetString(ignoreFileBytes);

				ignoreFiles = new List<string> { StaticIgnoreFile };

				// we don't want to lose trailing whitespace on linux
				using (var reader = new StringReader(ignoreFileText))
				{
					cancellationToken.ThrowIfCancellationRequested();
					var line = await reader.ReadLineAsync(cancellationToken);
					if (!String.IsNullOrEmpty(line))
						ignoreFiles.Add(line);
				}

				var filesSymlinkTask = SymlinkBase(true);
				var dirsSymlinkTask = SymlinkBase(false);
				await ValueTaskExtensions.WhenAll(filesSymlinkTask, dirsSymlinkTask);
			}
		}

		/// <inheritdoc />
		public async ValueTask<ConfigurationFileResponse?> Write(string configurationRelativePath, ISystemIdentity? systemIdentity, string? previousHash, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			logger.LogTrace("Starting write to {path}", path);

			ConfigurationFileResponse? result = null;

			void WriteImpl()
			{
				try
				{
					var fileTicket = fileTransferService.CreateUpload(FileUploadStreamKind.ForSynchronousIO);
					var uploadCancellationToken = stoppingCts.Token;
					async Task UploadHandler()
					{
						await using (fileTicket)
						{
							var fileHash = previousHash;
							logger.LogTrace("Write to {path} waiting for upload stream", path);
							var uploadStream = await fileTicket.GetResult(uploadCancellationToken);
							if (uploadStream == null)
							{
								logger.LogTrace("Write to {path} expired", path);
								return; // expired
							}

							logger.LogTrace("Write to {path} received stream of length {length}...", path, uploadStream.Length);
							bool success = false;
							void WriteCallback()
							{
								logger.LogTrace("Running synchronous write...");
								success = synchronousIOManager.WriteFileChecked(path, uploadStream, ref fileHash, uploadCancellationToken);
								logger.LogTrace("Finished write {un}successfully!", success ? String.Empty : "un");
							}

							if (fileTicket == null)
							{
								logger.LogDebug("File upload ticket for {path} expired!", path);
								return;
							}

							using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
							{
								if (!locked)
								{
									fileTicket.SetError(ErrorCode.ConfigurationContendedAccess, null);
									return;
								}

								logger.LogTrace("Kicking off write callback");
								if (systemIdentity == null)
									await Task.Factory.StartNew(WriteCallback, uploadCancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
								else
									await systemIdentity.RunImpersonated(WriteCallback, uploadCancellationToken);
							}

							if (!success)
								fileTicket.SetError(ErrorCode.ConfigurationFileUpdated, fileHash);
							else if (uploadStream.Length > 0)
								postWriteHandler.HandleWrite(path);
							else
								logger.LogTrace("Write complete");
						}
					}

					result = new ConfigurationFileResponse
					{
						FileTicket = fileTicket.Ticket.FileTicket,
						LastReadHash = previousHash,
						IsDirectory = false,
						AccessDenied = false,
						Path = configurationRelativePath,
					};

					lock (stoppingCts)
					{
						async Task ChainUploadTasks()
						{
							var oldUploadTask = uploadTasks;
							var newUploadTask = UploadHandler();
							try
							{
								await oldUploadTask;
							}
							finally
							{
								await newUploadTask;
							}
						}

						uploadTasks = ChainUploadTasks();
					}
				}
				catch (UnauthorizedAccessException)
				{
					// this happens on windows, dunno about linux
					bool isDirectory;
					try
					{
						isDirectory = synchronousIOManager.IsDirectory(path);
					}
					catch (Exception ex)
					{
						logger.LogDebug(ex, "IsDirectory exception!");
						isDirectory = false;
					}

					result = new ConfigurationFileResponse
					{
						Path = configurationRelativePath,
					};
					if (!isDirectory)
						result.AccessDenied = true;

					result.IsDirectory = isDirectory;
				}
			}

			using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
			{
				if (!locked)
				{
					logger.LogDebug("Contention when attempting to write file!");
					return null;
				}

				if (systemIdentity == null)
					await Task.Factory.StartNew(WriteImpl, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
				else
					await systemIdentity.RunImpersonated(WriteImpl, cancellationToken);
			}

			return result;
		}

		/// <inheritdoc />
		public async ValueTask<bool?> CreateDirectory(string configurationRelativePath, ISystemIdentity? systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			bool? result = null;
			void DoCreate() => result = synchronousIOManager.CreateDirectory(path, cancellationToken);

			using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
			{
				if (!locked)
				{
					logger.LogDebug("Contention when attempting to create directory!");
					return null;
				}

				if (systemIdentity == null)
					await Task.Factory.StartNew(DoCreate, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);
				else
					await systemIdentity.RunImpersonated(DoCreate, cancellationToken);
			}

			return result!.Value;
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => EnsureDirectories(cancellationToken);

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);

			stoppingCts.Cancel();
			try
			{
				await uploadTasks;
			}
			catch (OperationCanceledException ex)
			{
				logger.LogDebug(ex, "One or more uploads/downloads were aborted!");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error awaiting upload tasks!");
			}
		}

		/// <inheritdoc />
		public ValueTask HandleEvent(EventType eventType, IEnumerable<string?> parameters, bool deploymentPipeline, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			if (!EventTypeScriptFileNameMap.TryGetValue(eventType, out var scriptNames))
			{
				logger.LogTrace("No event script for event {event}!", eventType);
				return ValueTask.CompletedTask;
			}

			return ExecuteEventScripts(parameters, deploymentPipeline, cancellationToken, scriptNames);
		}

		/// <inheritdoc />
		public ValueTask? HandleCustomEvent(string scriptName, IEnumerable<string?> parameters, CancellationToken cancellationToken)
		{
			var scriptNameIsTgsEventName = EventTypeScriptFileNameMap
				.Values
				.SelectMany(scriptNames => scriptNames)
				.Any(tgsScriptName => tgsScriptName.Equals(
					scriptName,
					platformIdentifier.IsWindows
						? StringComparison.OrdinalIgnoreCase
						: StringComparison.Ordinal));
			if (scriptNameIsTgsEventName)
			{
				logger.LogWarning("DMAPI attempted to execute TGS reserved event: {eventName}", scriptName);
				return null;
			}

#pragma warning disable CA2012 // Use ValueTasks correctly
			return ExecuteEventScripts(parameters, false, cancellationToken, scriptName);
#pragma warning restore CA2012 // Use ValueTasks correctly
		}

		/// <inheritdoc />
		public async ValueTask<bool?> DeleteDirectory(string configurationRelativePath, ISystemIdentity? systemIdentity, CancellationToken cancellationToken)
		{
			await EnsureDirectories(cancellationToken);
			var path = ValidateConfigRelativePath(configurationRelativePath);

			var result = false;
			using (SemaphoreSlimContext.TryLock(semaphore, logger, out var locked))
			{
				if (!locked)
				{
					logger.LogDebug("Contention when attempting to enumerate directory!");
					return null;
				}

				void CheckDeleteImpl() => result = synchronousIOManager.DeleteDirectory(path);

				if (systemIdentity != null)
					await systemIdentity.RunImpersonated(CheckDeleteImpl, cancellationToken);
				else
					CheckDeleteImpl();
			}

			return result;
		}

		/// <summary>
		/// Get the proper path to <see cref="StaticIgnoreFile"/>.
		/// </summary>
		/// <returns>The <see cref="ioManager"/> relative path to <see cref="StaticIgnoreFile"/>.</returns>
		string StaticIgnorePath() => ioManager.ConcatPath(GameStaticFilesSubdirectory, StaticIgnoreFile);

		/// <summary>
		/// Ensures standard configuration directories exist.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task EnsureDirectories(CancellationToken cancellationToken)
		{
			async Task ValidateStaticFolder()
			{
				await ioManager.CreateDirectory(GameStaticFilesSubdirectory, cancellationToken);
				var staticIgnorePath = StaticIgnorePath();
				if (!await ioManager.FileExists(staticIgnorePath, cancellationToken))
					await ioManager.WriteAllBytes(staticIgnorePath, Array.Empty<byte>(), cancellationToken);
			}

			async Task ValidateCodeModsFolder()
			{
				if (await ioManager.DirectoryExists(CodeModificationsSubdirectory, cancellationToken))
					return;

				await ioManager.CreateDirectory(CodeModificationsSubdirectory, cancellationToken);
				var headWriteTask = ioManager.WriteAllBytes(
					ioManager.ConcatPath(
						CodeModificationsSubdirectory,
						CodeModificationsHeadFile),
					Encoding.UTF8.GetBytes(DefaultHeadInclude),
					cancellationToken);
				var tailWriteTask = ioManager.WriteAllBytes(
					ioManager.ConcatPath(
						CodeModificationsSubdirectory,
						CodeModificationsTailFile),
					Encoding.UTF8.GetBytes(DefaultTailInclude),
					cancellationToken);
				await ValueTaskExtensions.WhenAll(headWriteTask, tailWriteTask);
			}

			return Task.WhenAll(
				ValidateCodeModsFolder(),
				ioManager.CreateDirectory(EventScriptsSubdirectory, cancellationToken),
				ValidateStaticFolder());
		}

		/// <summary>
		/// Resolve a given <paramref name="configurationRelativePath"/> to it's full path or throw an <see cref="InvalidOperationException"/> if it violates rules.
		/// </summary>
		/// <param name="configurationRelativePath">A relative path in the instance's configuration directory.</param>
		/// <returns>The full on-disk path of <paramref name="configurationRelativePath"/>.</returns>
		string ValidateConfigRelativePath(string? configurationRelativePath)
		{
			var nullOrEmptyCheck = String.IsNullOrEmpty(configurationRelativePath);
			if (nullOrEmptyCheck)
				configurationRelativePath = DefaultIOManager.CurrentDirectory;
			if (configurationRelativePath![0] == Path.DirectorySeparatorChar || configurationRelativePath[0] == Path.AltDirectorySeparatorChar)
				configurationRelativePath = DefaultIOManager.CurrentDirectory + configurationRelativePath;
			var resolved = ioManager.ResolvePath(configurationRelativePath);
			var local = !nullOrEmptyCheck ? ioManager.ResolvePath() : null;
			if (!nullOrEmptyCheck && resolved.Length < local!.Length) // .. fuccbois
				throw new InvalidOperationException("Attempted to access file outside of configuration manager!");
			return resolved;
		}

		/// <summary>
		/// Execute a set of given <paramref name="scriptNames"/>.
		/// </summary>
		/// <param name="parameters">An <see cref="IEnumerable{T}"/> of <see cref="string"/> parameters for the <paramref name="scriptNames"/>.</param>
		/// <param name="deploymentPipeline">If this event is part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <param name="scriptNames">The names of the scripts to execute.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask ExecuteEventScripts(IEnumerable<string?> parameters, bool deploymentPipeline, CancellationToken cancellationToken, params string[] scriptNames)
		{
			await EnsureDirectories(cancellationToken);

			// always execute in serial
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken, logger))
			{
				var directories = generalConfiguration.AdditionalEventScriptsDirectories?.ToList() ?? new List<string>();
				directories.Add(EventScriptsSubdirectory);

				var allScripts = new List<string>();
				var tasks = directories.Select<string, ValueTask>(
					async scriptDirectory =>
					{
						var resolvedScriptsDir = ioManager.ResolvePath(scriptDirectory);
						logger.LogTrace("Checking for scripts in {directory}...", scriptDirectory);
						var files = await ioManager.GetFilesWithExtension(scriptDirectory, platformIdentifier.ScriptFileExtension, false, cancellationToken);

						var scriptFiles = files
							.Select(ioManager.GetFileName)
							.Where(x => scriptNames.Any(
								scriptName => x.StartsWith(scriptName, StringComparison.Ordinal)))
							.Select(x =>
							{
								var fullScriptPath = ioManager.ConcatPath(resolvedScriptsDir, x);
								logger.LogTrace("Found matching script: {scriptPath}", fullScriptPath);
								return fullScriptPath;
							});

						lock (allScripts)
							allScripts.AddRange(scriptFiles);
					})
					.ToList();

				await ValueTaskExtensions.WhenAll(tasks);
				if (allScripts.Count == 0)
				{
					logger.LogTrace("No event scripts starting with \"{scriptName}\" detected", String.Join("\" or \"", scriptNames));
					return;
				}

				var resolvedInstanceScriptsDir = ioManager.ResolvePath(EventScriptsSubdirectory);

				foreach (var scriptFile in allScripts.OrderBy(ioManager.GetFileName))
				{
					logger.LogTrace("Running event script {scriptFile}...", scriptFile);
					await using (var script = await processExecutor.LaunchProcess(
						scriptFile,
						resolvedInstanceScriptsDir,
						String.Join(
							' ',
							parameters.Select(arg =>
							{
								if (arg == null)
									return "(NULL)";

								if (!arg.Contains(' ', StringComparison.Ordinal))
									return arg;

								arg = arg.Replace("\"", "\\\"", StringComparison.Ordinal);

								return $"\"{arg}\"";
							})),
						cancellationToken,
						readStandardHandles: true,
						noShellExecute: true))
					using (cancellationToken.Register(() => script.Terminate()))
					{
						if (sessionConfiguration.LowPriorityDeploymentProcesses && deploymentPipeline)
							script.AdjustPriority(false);

						var exitCode = await script.Lifetime;
						cancellationToken.ThrowIfCancellationRequested();
						var scriptOutput = await script.GetCombinedOutput(cancellationToken);
						if (exitCode != 0)
							throw new JobException($"Script {scriptFile} exited with code {exitCode}:{Environment.NewLine}{scriptOutput}");
						else
							logger.LogDebug("Script output:{newLine}{scriptOutput}", Environment.NewLine, scriptOutput);
					}
				}
			}
		}
	}
}
