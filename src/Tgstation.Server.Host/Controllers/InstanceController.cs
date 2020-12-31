using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="Components.Instance"/>s
	/// </summary>
	[Route(Routes.InstanceManager)]
	#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class InstanceController : ApiController
	{
		/// <summary>
		/// File name to allow attaching instances.
		/// </summary>
		public const string InstanceAttachFileName = "TGS4_ALLOW_INSTANCE_ATTACH";

		/// <summary>
		/// Prefix for move <see cref="Api.Models.Job"/>s.
		/// </summary>
		const string MoveInstanceJobPrefix = "Move instance ID ";

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IPortAllocator"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IPortAllocator portAllocator;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Construct a <see cref="InstanceController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/></param>
		/// <param name="portAllocator">The value of <see cref="IPortAllocator"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		public InstanceController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IJobManager jobManager,
			IInstanceManager instanceManager,
			IIOManager ioManager,
			IPortAllocator portAllocator,
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<InstanceController> logger)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
		}

		async Task<Models.Instance> CreateDefaultInstance(Api.Models.Instance initialSettings, CancellationToken cancellationToken)
		{
			var ddPort = await portAllocator.GetAvailablePort(1, false, cancellationToken).ConfigureAwait(false);
			if (!ddPort.HasValue)
				return null;

			// try to use the old default if possible
			const ushort DefaultDreamDaemonPort = 1337;
			if (ddPort.Value < DefaultDreamDaemonPort)
				ddPort = await portAllocator.GetAvailablePort(DefaultDreamDaemonPort, false, cancellationToken).ConfigureAwait(false) ?? ddPort;

			const ushort DefaultApiValidationPort = 1339;
			var dmPort = await portAllocator
				.GetAvailablePort(
					Math.Min((ushort)(ddPort.Value + 1), DefaultApiValidationPort),
					false,
					cancellationToken)
				.ConfigureAwait(false);
			if (!dmPort.HasValue)
				return null;

			// try to use the old default if possible
			if (dmPort < DefaultApiValidationPort)
				dmPort = await portAllocator.GetAvailablePort(DefaultApiValidationPort, false, cancellationToken).ConfigureAwait(false) ?? dmPort;

			return new Models.Instance
			{
				ConfigurationType = initialSettings.ConfigurationType ?? ConfigurationType.Disallowed,
				DreamDaemonSettings = new DreamDaemonSettings
				{
					AllowWebClient = false,
					AutoStart = false,
					Port = ddPort,
					SecurityLevel = DreamDaemonSecurity.Safe,
					StartupTimeout = 60,
					HeartbeatSeconds = 60,
					TopicRequestTimeout = generalConfiguration.ByondTopicTimeout,
					AdditionalParameters = String.Empty,
				},
				DreamMakerSettings = new DreamMakerSettings
				{
					ApiValidationPort = dmPort,
					ApiValidationSecurityLevel = DreamDaemonSecurity.Safe,
					RequireDMApiValidation = true
				},
				Name = initialSettings.Name,
				Online = false,
				Path = initialSettings.Path,
				AutoUpdateInterval = initialSettings.AutoUpdateInterval ?? 0,
				ChatBotLimit = initialSettings.ChatBotLimit ?? Models.Instance.DefaultChatBotLimit,
				RepositorySettings = new RepositorySettings
				{
					CommitterEmail = Components.Repository.Repository.DefaultCommitterEmail,
					CommitterName = Components.Repository.Repository.DefaultCommitterName,
					PushTestMergeCommits = false,
					ShowTestMergeCommitters = false,
					AutoUpdatesKeepTestMerges = false,
					AutoUpdatesSynchronize = false,
					PostTestMergeComment = false,
					CreateGitHubDeployments = false
				},
				InstancePermissionSets = new List<Models.InstancePermissionSet> // give this user full privileges on the instance
				{
					InstanceAdminPermissionSet(null)
				},
				SwarmIdentifer = swarmConfiguration.Identifier,
			};
		}

		string NormalizePath(string path)
		{
			if (path == null)
				return null;

			path = ioManager.ResolvePath(path);
			if (platformIdentifier.IsWindows)
				path = path.ToUpperInvariant().Replace('\\', '/');

			return path;
		}

		Models.InstancePermissionSet InstanceAdminPermissionSet(Models.InstancePermissionSet permissionSetToModify)
		{
			if (permissionSetToModify == null)
				permissionSetToModify = new Models.InstancePermissionSet()
				{
					PermissionSetId = AuthenticationContext.PermissionSet.Id.Value
				};
			permissionSetToModify.ByondRights = RightsHelper.AllRights<ByondRights>();
			permissionSetToModify.ChatBotRights = RightsHelper.AllRights<ChatBotRights>();
			permissionSetToModify.ConfigurationRights = RightsHelper.AllRights<ConfigurationRights>();
			permissionSetToModify.DreamDaemonRights = RightsHelper.AllRights<DreamDaemonRights>();
			permissionSetToModify.DreamMakerRights = RightsHelper.AllRights<DreamMakerRights>();
			permissionSetToModify.RepositoryRights = RightsHelper.AllRights<RepositoryRights>();
			permissionSetToModify.InstancePermissionSetRights = RightsHelper.AllRights<InstancePermissionSetRights>();
			return permissionSetToModify;
		}

		/// <summary>
		/// Create or attach an <see cref="Api.Models.Instance"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.Instance"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Instance attached successfully.</response>
		/// <response code="201">Instance created successfully.</response>
		[HttpPut]
		[TgsAuthorize(InstanceManagerRights.Create)]
		[ProducesResponseType(typeof(Api.Models.Instance), 200)]
		[ProducesResponseType(typeof(Api.Models.Instance), 201)]
		public async Task<IActionResult> Create([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new ErrorMessage(ErrorCode.InstanceWhitespaceName));

			var unNormalizedPath = model.Path;
			var targetInstancePath = NormalizePath(unNormalizedPath);
			model.Path = targetInstancePath;

			var installationDirectoryPath = NormalizePath(DefaultIOManager.CurrentDirectory);

			bool InstanceIsChildOf(string otherPath)
			{
				if (!targetInstancePath.StartsWith(otherPath, StringComparison.Ordinal))
					return false;

				bool sameLength = targetInstancePath.Length == otherPath.Length;
				char dirSeparatorChar = targetInstancePath.ToCharArray()[Math.Min(otherPath.Length, targetInstancePath.Length - 1)];
				return sameLength
					|| dirSeparatorChar == Path.DirectorySeparatorChar
					|| dirSeparatorChar == Path.AltDirectorySeparatorChar;
			}

			if (InstanceIsChildOf(installationDirectoryPath))
				return Conflict(new ErrorMessage(ErrorCode.InstanceAtConflictingPath));

			// Validate it's not a child of any other instance
			IActionResult earlyOut = null;
			ulong countOfOtherInstances = 0;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				var newCancellationToken = cts.Token;
				try
				{
					await DatabaseContext
						.Instances
						.AsQueryable()
						.Where(x => x.SwarmIdentifer == swarmConfiguration.Identifier)
						.Select(x => new Models.Instance
						{
							Path = x.Path
						})
						.ForEachAsync(
							otherInstance =>
							{
								if (++countOfOtherInstances >= generalConfiguration.InstanceLimit)
									earlyOut ??= Conflict(new ErrorMessage(ErrorCode.InstanceLimitReached));
								else if (InstanceIsChildOf(otherInstance.Path))
									earlyOut ??= Conflict(new ErrorMessage(ErrorCode.InstanceAtConflictingPath));

								if (earlyOut != null && !newCancellationToken.IsCancellationRequested)
									cts.Cancel();
							},
							newCancellationToken)
						.ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}

			if (earlyOut != null)
				return earlyOut;

			// Last test, ensure it's in the list of valid paths
			if (!(generalConfiguration.ValidInstancePaths?
				.Select(path => NormalizePath(path))
				.Any(path => InstanceIsChildOf(path)) ?? true))
				return BadRequest(new ErrorMessage(ErrorCode.InstanceNotAtWhitelistedPath));

			async Task<bool> DirExistsAndIsNotEmpty()
			{
				if (!await ioManager.DirectoryExists(model.Path, cancellationToken).ConfigureAwait(false))
					return false;

				var filesTask = ioManager.GetFiles(model.Path, cancellationToken);
				var dirsTask = ioManager.GetDirectories(model.Path, cancellationToken);

				var files = await filesTask.ConfigureAwait(false);
				var dirs = await dirsTask.ConfigureAwait(false);

				return files.Concat(dirs).Any();
			}

			var dirExistsTask = DirExistsAndIsNotEmpty();
			bool attached = false;
			if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
				if (!await ioManager.FileExists(ioManager.ConcatPath(model.Path, InstanceAttachFileName), cancellationToken).ConfigureAwait(false))
					return Conflict(new ErrorMessage(ErrorCode.InstanceAtExistingPath));
				else
					attached = true;

			var newInstance = await CreateDefaultInstance(model, cancellationToken).ConfigureAwait(false);
			if (newInstance == null)
				return Conflict(new ErrorMessage(ErrorCode.NoPortsAvailable));

			DatabaseContext.Instances.Add(newInstance);
			try
			{
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

				try
				{
					// actually reserve it now
					await ioManager.CreateDirectory(unNormalizedPath, cancellationToken).ConfigureAwait(false);
					await ioManager.DeleteFile(ioManager.ConcatPath(targetInstancePath, InstanceAttachFileName), cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					// oh shit delete the model
					DatabaseContext.Instances.Remove(newInstance);

					// DCT: Operation must always run
					await DatabaseContext.Save(default).ConfigureAwait(false);
					throw;
				}
			}
			catch (IOException e)
			{
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					AdditionalData = e.Message
				});
			}

			Logger.LogInformation("{0} {1} instance {2}: {3} ({4})", AuthenticationContext.User.Name, attached ? "attached" : "created", newInstance.Name, newInstance.Id, newInstance.Path);

			var api = newInstance.ToApi();
			return attached ? (IActionResult)Json(api) : Created(api);
		}

		/// <summary>
		/// Detach an <see cref="Api.Models.Instance"/> with the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the instance to detach.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Instance detatched successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(InstanceManagerRights.Delete)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var originalModel = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == id && x.SwarmIdentifer == swarmConfiguration.Identifier)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (originalModel == default)
				return Gone();
			if (originalModel.Online.Value)
				return Conflict(new ErrorMessage(ErrorCode.InstanceDetachOnline));

			DatabaseContext.Instances.Remove(originalModel);

			var attachFileName = ioManager.ConcatPath(originalModel.Path, InstanceAttachFileName);
			try
			{
				await ioManager.WriteAllBytes(attachFileName, Array.Empty<byte>(), cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// DCT: Operation must always run
				await ioManager.DeleteFile(attachFileName, default).ConfigureAwait(false);
				throw;
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false); // cascades everything
			return NoContent();
		}

		/// <summary>
		/// Modify an <see cref="Api.Models.Instance"/>'s settings.
		/// </summary>
		/// <param name="model">The updated <see cref="Api.Models.Instance"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Instance updated successfully.</response>
		/// <response code="202">Instance updated successfully and relocation job created.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(InstanceManagerRights.Relocate | InstanceManagerRights.Rename | InstanceManagerRights.SetAutoUpdate | InstanceManagerRights.SetConfiguration | InstanceManagerRights.SetOnline | InstanceManagerRights.SetChatBotLimit)]
		[ProducesResponseType(typeof(Api.Models.Instance), 200)]
		[ProducesResponseType(typeof(Api.Models.Instance), 202)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
#pragma warning disable CA1502 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			IQueryable<Models.Instance> InstanceQuery() => DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == model.Id && x.SwarmIdentifer == swarmConfiguration.Identifier);

			var moveJob = await InstanceQuery()
				.SelectMany(x => x.Jobs).
#pragma warning disable CA1310 // Specify StringComparison
				Where(x => !x.StoppedAt.HasValue && x.Description.StartsWith(MoveInstanceJobPrefix))
#pragma warning restore CA1310 // Specify StringComparison
				.Select(x => new Models.Job
				{
					Id = x.Id
				}).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (moveJob != default)
			{
				// don't allow them to cancel it if they can't start it.
				if (!AuthenticationContext.PermissionSet.InstanceManagerRights.Value.HasFlag(InstanceManagerRights.Relocate))
					return Forbid();
				await jobManager.CancelJob(moveJob, AuthenticationContext.User, true, cancellationToken).ConfigureAwait(false); // cancel it now
			}

			var originalModel = await InstanceQuery()
				.Include(x => x.RepositorySettings)
				.Include(x => x.ChatSettings)
				.ThenInclude(x => x.Channels)
				.Include(x => x.DreamDaemonSettings) // need these for onlining
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (originalModel == default(Models.Instance))
				return Gone();

			if (InstanceRequiredController.ValidateInstanceOnlineStatus(instanceManager, Logger, originalModel))
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			var userRights = (InstanceManagerRights)AuthenticationContext.GetRight(RightsType.InstanceManager);
			bool CheckModified<T>(Expression<Func<Api.Models.Instance, T>> expression, InstanceManagerRights requiredRight)
			{
				var memberSelectorExpression = (MemberExpression)expression.Body;
				var property = (PropertyInfo)memberSelectorExpression.Member;

				var newVal = property.GetValue(model);
				if (newVal == null)
					return false;
				if (!userRights.HasFlag(requiredRight) && property.GetValue(originalModel) != newVal)
					return true;

				property.SetValue(originalModel, newVal);
				return false;
			}

			string originalModelPath = null;
			string rawPath = null;
			if (model.Path != null)
			{
				rawPath = NormalizePath(model.Path);

				if (rawPath != originalModel.Path)
				{
					if (!userRights.HasFlag(InstanceManagerRights.Relocate))
						return Forbid();
					if (originalModel.Online.Value && model.Online != true)
						return Conflict(new ErrorMessage(ErrorCode.InstanceRelocateOnline));

					var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
					if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
						return Conflict(new ErrorMessage(ErrorCode.InstanceAtExistingPath));

					originalModelPath = originalModel.Path;
					originalModel.Path = rawPath;
				}
			}

			var oldAutoUpdateInterval = originalModel.AutoUpdateInterval.Value;
			var originalOnline = originalModel.Online.Value;
			var renamed = model.Name != null && originalModel.Name != model.Name;

			if (CheckModified(x => x.AutoUpdateInterval, InstanceManagerRights.SetAutoUpdate)
				|| CheckModified(x => x.ConfigurationType, InstanceManagerRights.SetConfiguration)
				|| CheckModified(x => x.Name, InstanceManagerRights.Rename)
				|| CheckModified(x => x.Online, InstanceManagerRights.SetOnline)
				|| CheckModified(x => x.ChatBotLimit, InstanceManagerRights.SetChatBotLimit))
				return Forbid();

			if (model.ChatBotLimit.HasValue)
			{
				var countOfExistingChatBots = await DatabaseContext
					.ChatBots
					.AsQueryable()
					.Where(x => x.InstanceId == originalModel.Id)
					.CountAsync(cancellationToken)
					.ConfigureAwait(false);

				if (countOfExistingChatBots > model.ChatBotLimit.Value)
					return Conflict(new ErrorMessage(ErrorCode.ChatBotMax));
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if (renamed)
			{
				using var componentInstance = instanceManager.GetInstanceReference(originalModel);
				if (componentInstance != null)
					await componentInstance.InstanceRenamed(originalModel.Name, cancellationToken).ConfigureAwait(false);
			}

			var oldAutoStart = originalModel.DreamDaemonSettings.AutoStart;
			try
			{
				if (originalOnline && model.Online == false)
					await instanceManager.OfflineInstance(originalModel, AuthenticationContext.User, cancellationToken).ConfigureAwait(false);
				else if (!originalOnline && model.Online == true)
				{
					// force autostart false here because we don't want any long running jobs right now
					// remember to document this
					originalModel.DreamDaemonSettings.AutoStart = false;
					await instanceManager.OnlineInstance(originalModel, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				if(!(e is OperationCanceledException))
					Logger.LogError(e, "Error changing instance online state!");
				originalModel.Online = originalOnline;
				originalModel.DreamDaemonSettings.AutoStart = oldAutoStart;
				if (originalModelPath != null)
					originalModel.Path = originalModelPath;

				// DCT: Operation must always run
				await DatabaseContext.Save(default).ConfigureAwait(false);
				throw;
			}

			var api = (AuthenticationContext.GetRight(RightsType.InstanceManager) & (ulong)InstanceManagerRights.Read) != 0 ? originalModel.ToApi() : new Api.Models.Instance
			{
				Id = originalModel.Id
			};

			var moving = originalModelPath != null;
			if (moving)
			{
				var job = new Models.Job
				{
					Description = $"{MoveInstanceJobPrefix}{originalModel.Id} from {originalModelPath} to {rawPath}",
					Instance = originalModel,
					CancelRightsType = RightsType.InstanceManager,
					CancelRight = (ulong)InstanceManagerRights.Relocate,
					StartedBy = AuthenticationContext.User
				};

				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressHandler, ct) // core will be null here since the instance is offline
						=> instanceManager.MoveInstance(originalModel, originalModelPath, ct),
					cancellationToken)
					.ConfigureAwait(false);
				api.MoveJob = job.ToApi();
			}

			if (model.AutoUpdateInterval.HasValue && oldAutoUpdateInterval != model.AutoUpdateInterval)
			{
				using var componentInstance = instanceManager.GetInstanceReference(originalModel);
				if (componentInstance != null)
					await componentInstance.SetAutoUpdateInterval(model.AutoUpdateInterval.Value).ConfigureAwait(false);
			}

			return moving ? (IActionResult)Accepted(api) : Json(api);
		}
#pragma warning restore CA1502

		/// <summary>
		/// List <see cref="Api.Models.Instance"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Instance"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(InstanceManagerRights.List | InstanceManagerRights.Read)]
		[ProducesResponseType(typeof(Paginated<Api.Models.Instance>), 200)]
		public async Task<IActionResult> List(
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken)
		{
			IQueryable<Models.Instance> GetBaseQuery()
			{
				IQueryable<Models.Instance> query = DatabaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.SwarmIdentifer == swarmConfiguration.Identifier);
				if (!AuthenticationContext.PermissionSet.InstanceManagerRights.Value.HasFlag(InstanceManagerRights.List))
					query = query
						.Where(x => x.InstancePermissionSets.Any(y => y.PermissionSetId == AuthenticationContext.PermissionSet.Id.Value))
						.Where(x => x.InstancePermissionSets.Any(instanceUser =>
							instanceUser.ByondRights != ByondRights.None ||
							instanceUser.ChatBotRights != ChatBotRights.None ||
							instanceUser.ConfigurationRights != ConfigurationRights.None ||
							instanceUser.DreamDaemonRights != DreamDaemonRights.None ||
							instanceUser.DreamMakerRights != DreamMakerRights.None ||
							instanceUser.InstancePermissionSetRights != InstancePermissionSetRights.None));

				// Hack for EF IAsyncEnumerable BS
				return query.Select(x => x);
			}

			var moveJobs = await GetBaseQuery()
				.SelectMany(x => x.Jobs)
#pragma warning disable CA1310 // Specify StringComparison
				.Where(x => !x.StoppedAt.HasValue && x.Description.StartsWith(MoveInstanceJobPrefix))
#pragma warning restore CA1310 // Specify StringComparison
				.Include(x => x.StartedBy).ThenInclude(x => x.CreatedBy)
				.Include(x => x.Instance)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			var needsUpdate = false;
			var result = await Paginated<Models.Instance, Api.Models.Instance>(
				() => Task.FromResult(
					new PaginatableResult<Models.Instance>(
						GetBaseQuery())),
				instance =>
				{
					needsUpdate |= InstanceRequiredController.ValidateInstanceOnlineStatus(instanceManager, Logger, instance);
					instance.MoveJob = moveJobs.FirstOrDefault(x => x.Instance.Id == instance.Id)?.ToApi();
				},
				page,
				pageSize,
				cancellationToken)
				.ConfigureAwait(false);

			if (needsUpdate)
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.Instance"/>.
		/// </summary>
		/// <param name="id">The instance <see cref="EntityId.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Instance"/> successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(InstanceManagerRights.List | InstanceManagerRights.Read)]
		[ProducesResponseType(typeof(Api.Models.Instance), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var cantList = !AuthenticationContext.PermissionSet.InstanceManagerRights.Value.HasFlag(InstanceManagerRights.List);
			IQueryable<Models.Instance> QueryForUser()
			{
				var query = DatabaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.Id == id && x.SwarmIdentifer == swarmConfiguration.Identifier);

				if (cantList)
					query = query.Include(x => x.InstancePermissionSets);
				return query;
			}

			var instance = await QueryForUser().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (instance == null)
				return Gone();

			if (InstanceRequiredController.ValidateInstanceOnlineStatus(instanceManager, Logger, instance))
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if (cantList && !instance.InstancePermissionSets.Any(instanceUser => instanceUser.PermissionSetId == AuthenticationContext.PermissionSet.Id.Value &&
				(instanceUser.ByondRights != ByondRights.None ||
				instanceUser.ChatBotRights != ChatBotRights.None ||
				instanceUser.ConfigurationRights != ConfigurationRights.None ||
				instanceUser.DreamDaemonRights != DreamDaemonRights.None ||
				instanceUser.DreamMakerRights != DreamMakerRights.None ||
				instanceUser.InstancePermissionSetRights != InstancePermissionSetRights.None)))
				return Forbid();

			var api = instance.ToApi();

			var moveJob = await QueryForUser()
				.SelectMany(x => x.Jobs)
#pragma warning disable CA1310 // Specify StringComparison
				.Where(x => !x.StoppedAt.HasValue && x.Description.StartsWith(MoveInstanceJobPrefix))
#pragma warning restore CA1310 // Specify StringComparison
				.Include(x => x.StartedBy).ThenInclude(x => x.CreatedBy)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			api.MoveJob = moveJob?.ToApi();
			return Json(api);
		}

		/// <summary>
		/// Gives the current user full permissions on a given instance <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The instance <see cref="EntityId.Id"/> to give permissions on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Granted permissions successfully.</response>
		[HttpPatch("{id}")]
		[TgsAuthorize(InstanceManagerRights.GrantPermissions)]
		[ProducesResponseType(204)]
		public async Task<IActionResult> GrantPermissions(long id, CancellationToken cancellationToken)
		{
			// ensure the current user has write privilege on the instance
			var usersInstancePermissionSet = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == id && x.SwarmIdentifer == swarmConfiguration.Identifier)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == AuthenticationContext.PermissionSet.Id.Value)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (usersInstancePermissionSet == default)
			{
				var instanceAdminUser = InstanceAdminPermissionSet(null);
				instanceAdminUser.InstanceId = id;
				DatabaseContext.InstancePermissionSets.Add(instanceAdminUser);
			}
			else
				InstanceAdminPermissionSet(usersInstancePermissionSet);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			return NoContent();
		}
	}
}
