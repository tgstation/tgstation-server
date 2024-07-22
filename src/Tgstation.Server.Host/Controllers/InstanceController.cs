using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NCrontab;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="Components.Instance"/>s.
	/// </summary>
	[Route(Routes.InstanceManager)]
#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class InstanceController : ComponentInterfacingController
	{
		/// <summary>
		/// File name to allow attaching instances.
		/// </summary>
		public const string InstanceAttachFileName = "TGS4_ALLOW_INSTANCE_ATTACH";

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IPortAllocator"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IPortAllocator portAllocator;

		/// <summary>
		/// The <see cref="IPermissionsUpdateNotifyee"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly IPermissionsUpdateNotifyee permissionsUpdateNotifyee;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="InstanceController"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="portAllocator">The value of <see cref="portAllocator"/>.</param>
		/// <param name="permissionsUpdateNotifyee">The value of <see cref="permissionsUpdateNotifyee"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ComponentInterfacingController"/>.</param>
		public InstanceController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<InstanceController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IIOManager ioManager,
			IPortAllocator portAllocator,
			IPlatformIdentifier platformIdentifier,
			IPermissionsUpdateNotifyee permissionsUpdateNotifyee,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders,
				  false)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
			this.permissionsUpdateNotifyee = permissionsUpdateNotifyee ?? throw new ArgumentNullException(nameof(permissionsUpdateNotifyee));

			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
		}

		/// <summary>
		/// Create or attach an <see cref="Api.Models.Instance"/>.
		/// </summary>
		/// <param name="model">The <see cref="InstanceCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Instance attached successfully.</response>
		/// <response code="201">Instance created successfully.</response>
		[HttpPut]
		[TgsAuthorize(InstanceManagerRights.Create)]
		[ProducesResponseType(typeof(InstanceResponse), 200)]
		[ProducesResponseType(typeof(InstanceResponse), 201)]
		public async ValueTask<IActionResult> Create([FromBody] InstanceCreateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (String.IsNullOrWhiteSpace(model.Name) || String.IsNullOrWhiteSpace(model.Path))
				return BadRequest(new ErrorMessageResponse(ErrorCode.InstanceWhitespaceNameOrPath));

			IActionResult? earlyOut = ValidateCronSetting(model);
			if (earlyOut != null)
				return earlyOut;

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
				return Conflict(new ErrorMessageResponse(ErrorCode.InstanceAtConflictingPath));

			// Validate it's not a child of any other instance
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
							Path = x.Path,
						})
						.ForEachAsync(
							otherInstance =>
							{
								if (++countOfOtherInstances >= generalConfiguration.InstanceLimit)
									earlyOut ??= Conflict(new ErrorMessageResponse(ErrorCode.InstanceLimitReached));
								else if (InstanceIsChildOf(otherInstance.Path!))
									earlyOut ??= Conflict(new ErrorMessageResponse(ErrorCode.InstanceAtConflictingPath));

								if (earlyOut != null && !newCancellationToken.IsCancellationRequested)
									cts.Cancel();
							},
							newCancellationToken);
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
				return BadRequest(new ErrorMessageResponse(ErrorCode.InstanceNotAtWhitelistedPath));

			async ValueTask<bool> DirExistsAndIsNotEmpty()
			{
				if (!await ioManager.DirectoryExists(model.Path, cancellationToken))
					return false;

				var filesTask = ioManager.GetFiles(model.Path, cancellationToken);
				var dirsTask = ioManager.GetDirectories(model.Path, cancellationToken);

				var files = await filesTask;
				var dirs = await dirsTask;

				return files.Concat(dirs).Any();
			}

			var dirExistsTask = DirExistsAndIsNotEmpty();
			bool attached = false;
			if (await ioManager.FileExists(model.Path, cancellationToken) || await dirExistsTask)
				if (!await ioManager.FileExists(ioManager.ConcatPath(model.Path, InstanceAttachFileName), cancellationToken))
					return Conflict(new ErrorMessageResponse(ErrorCode.InstanceAtExistingPath));
				else
					attached = true;

			var newInstance = await CreateDefaultInstance(model, cancellationToken);
			if (newInstance == null)
				return Conflict(new ErrorMessageResponse(ErrorCode.NoPortsAvailable));

			DatabaseContext.Instances.Add(newInstance);
			try
			{
				await DatabaseContext.Save(cancellationToken);

				try
				{
					// actually reserve it now
					await ioManager.CreateDirectory(unNormalizedPath, cancellationToken);
					await ioManager.DeleteFile(ioManager.ConcatPath(targetInstancePath, InstanceAttachFileName), cancellationToken);
				}
				catch
				{
					// oh shit delete the model
					DatabaseContext.Instances.Remove(newInstance);

					// DCT: Operation must always run
					await DatabaseContext.Save(CancellationToken.None);
					throw;
				}
			}
			catch (IOException e)
			{
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = e.Message,
				});
			}

			Logger.LogInformation(
				"{userName} {attachedOrCreated} instance {instanceName}: {instanceId} ({instancePath})",
				AuthenticationContext.User.Name,
				attached ? "attached" : "created",
				newInstance.Name,
				newInstance.Id,
				newInstance.Path);

			await permissionsUpdateNotifyee.InstancePermissionSetCreated(
				newInstance.InstancePermissionSets.First(),
				cancellationToken);

			var api = newInstance.ToApi();
			api.Accessible = true; // instances are always accessible by their creator
			return attached ? Json(api) : Created(api);
		}

		/// <summary>
		/// Detach an <see cref="Api.Models.Instance"/> with the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the instance to detach.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Instance detatched successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(InstanceManagerRights.Delete)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var originalModel = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == id && x.SwarmIdentifer == swarmConfiguration.Identifier)
				.FirstOrDefaultAsync(cancellationToken);
			if (originalModel == default)
				return this.Gone();
			if (originalModel.Online!.Value)
				return Conflict(new ErrorMessageResponse(ErrorCode.InstanceDetachOnline));

			DatabaseContext.Instances.Remove(originalModel);

			var originalPath = originalModel.Path!;
			var attachFileName = ioManager.ConcatPath(originalPath, InstanceAttachFileName);
			try
			{
				if (await ioManager.DirectoryExists(originalPath, cancellationToken))
					await ioManager.WriteAllBytes(attachFileName, Array.Empty<byte>(), cancellationToken);
			}
			catch (OperationCanceledException)
			{
				// DCT: Operation must always run
				await ioManager.DeleteFile(attachFileName, CancellationToken.None);
				throw;
			}

			await DatabaseContext.Save(cancellationToken); // cascades everything
			return NoContent();
		}

		/// <summary>
		/// Modify an <see cref="Api.Models.Instance"/>'s settings.
		/// </summary>
		/// <param name="model">The updated <see cref="Api.Models.Instance"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Instance updated successfully.</response>
		/// <response code="202">Instance updated successfully and relocation job created.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(InstanceManagerRights.Relocate | InstanceManagerRights.Rename | InstanceManagerRights.SetAutoUpdate | InstanceManagerRights.SetConfiguration | InstanceManagerRights.SetOnline | InstanceManagerRights.SetChatBotLimit)]
		[ProducesResponseType(typeof(InstanceResponse), 200)]
		[ProducesResponseType(typeof(InstanceResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1502 // TODO: Decomplexify
		public async ValueTask<IActionResult> Update([FromBody] InstanceUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			IQueryable<Models.Instance> InstanceQuery() => DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == model.Id && x.SwarmIdentifer == swarmConfiguration.Identifier);

			var moveJob = await InstanceQuery()
				.SelectMany(x => x.Jobs)
				.Where(x => !x.StoppedAt.HasValue && x.JobCode == JobCode.Move)
				.Select(x => new Job(x.Id!.Value))
				.FirstOrDefaultAsync(cancellationToken);

			if (moveJob != null)
			{
				// don't allow them to cancel it if they can't start it.
				if (!AuthenticationContext.PermissionSet.InstanceManagerRights!.Value.HasFlag(InstanceManagerRights.Relocate))
					return Forbid();
				await jobManager.CancelJob(moveJob, AuthenticationContext.User, true, cancellationToken); // cancel it now
			}

			var originalModel = await InstanceQuery()
				.Include(x => x.RepositorySettings)
				.Include(x => x.ChatSettings)
					.ThenInclude(x => x.Channels)
				.Include(x => x.DreamDaemonSettings) // need these for onlining
				.FirstOrDefaultAsync(cancellationToken);
			if (originalModel == default(Models.Instance))
				return this.Gone();

			if (ValidateInstanceOnlineStatus(originalModel))
				await DatabaseContext.Save(cancellationToken);

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

			string? originalModelPath = null;
			string? rawPath = null;
			var originalOnline = originalModel.Online!.Value;
			if (model.Path != null)
			{
				rawPath = NormalizePath(model.Path);

				if (rawPath != originalModel.Path)
				{
					if (!userRights.HasFlag(InstanceManagerRights.Relocate))
						return Forbid();
					if (originalOnline && model.Online != true)
						return Conflict(new ErrorMessageResponse(ErrorCode.InstanceRelocateOnline));

					var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
					if (await ioManager.FileExists(model.Path, cancellationToken) || await dirExistsTask)
						return Conflict(new ErrorMessageResponse(ErrorCode.InstanceAtExistingPath));

					originalModelPath = originalModel.Path;
					originalModel.Path = rawPath;
				}
			}

			var oldAutoUpdateInterval = originalModel.AutoUpdateInterval!.Value;
			var oldAutoUpdateCron = originalModel.AutoUpdateCron;

			var earlyOut = ValidateCronSetting(model);
			if (earlyOut != null)
				return earlyOut;

			var changedAutoInterval = model.AutoUpdateInterval.HasValue && oldAutoUpdateInterval != model.AutoUpdateInterval;
			var changedAutoCron = model.AutoUpdateCron != null && oldAutoUpdateCron != model.AutoUpdateCron;

			var renamed = model.Name != null && originalModel.Name != model.Name;

			if (CheckModified(x => x.AutoUpdateInterval, InstanceManagerRights.SetAutoUpdate)
				|| CheckModified(x => x.AutoUpdateCron, InstanceManagerRights.SetAutoUpdate)
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
					.CountAsync(cancellationToken);

				if (countOfExistingChatBots > model.ChatBotLimit.Value)
					return Conflict(new ErrorMessageResponse(ErrorCode.ChatBotMax));
			}

			if (changedAutoCron)
				model.AutoUpdateInterval = 0;
			else if (changedAutoInterval)
				model.AutoUpdateCron = String.Empty;

			await DatabaseContext.Save(cancellationToken);

			if (renamed)
			{
				// ignoring retval because we don't care if it's offline
				await WithComponentInstanceNullable(
					async componentInstance =>
					{
						await componentInstance.InstanceRenamed(originalModel.Name!, cancellationToken);
						return null;
					},
					originalModel);
			}

			var oldAutoStart = originalModel.DreamDaemonSettings!.AutoStart;
			try
			{
				if (originalOnline && model.Online == false)
					await InstanceOperations.OfflineInstance(originalModel, AuthenticationContext.User, cancellationToken);
				else if (!originalOnline && model.Online == true)
				{
					// force autostart false here because we don't want any long running jobs right now
					// remember to document this
					originalModel.DreamDaemonSettings.AutoStart = false;
					await InstanceOperations.OnlineInstance(originalModel, cancellationToken);
				}
			}
			catch (Exception e)
			{
				if (e is not OperationCanceledException)
					Logger.LogError(e, "Error changing instance online state!");
				originalModel.Online = originalOnline;
				originalModel.DreamDaemonSettings.AutoStart = oldAutoStart;
				if (originalModelPath != null)
					originalModel.Path = originalModelPath;

				// DCT: Operation must always run
				await DatabaseContext.Save(CancellationToken.None);
				throw;
			}

			var api = (AuthenticationContext.GetRight(RightsType.InstanceManager) & (ulong)InstanceManagerRights.Read) != 0 ? originalModel.ToApi() : new InstanceResponse
			{
				Id = originalModel.Id,
			};

			var moving = originalModelPath != null;
			if (moving)
			{
				var description = $"Move instance ID {originalModel.Id} from {originalModelPath} to {rawPath}";
				var job = Job.Create(JobCode.Move, AuthenticationContext.User, originalModel, InstanceManagerRights.Relocate);
				job.Description = description;

				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressHandler, ct) // core will be null here since the instance is offline
						=> InstanceOperations.MoveInstance(originalModel, originalModelPath!, ct),
					cancellationToken);
				api.MoveJob = job.ToApi();
			}

			if (changedAutoInterval || changedAutoCron)
			{
				// ignoring retval because we don't care if it's offline
				await WithComponentInstanceNullable(
					async componentInstance =>
					{
						await componentInstance.ScheduleAutoUpdate(model.AutoUpdateInterval!.Value, model.AutoUpdateCron);
						return null;
					},
					originalModel);
			}

			await CheckAccessible(api, cancellationToken);
			return moving ? Accepted(api) : Json(api);
		}
#pragma warning restore CA1502

		/// <summary>
		/// List <see cref="Api.Models.Instance"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Instance"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(InstanceManagerRights.List | InstanceManagerRights.Read)]
		[ProducesResponseType(typeof(PaginatedResponse<InstanceResponse>), 200)]
		public async ValueTask<IActionResult> List(
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken)
		{
			IQueryable<Models.Instance> GetBaseQuery()
			{
				var query = DatabaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.SwarmIdentifer == swarmConfiguration.Identifier);
				if (!AuthenticationContext.PermissionSet.InstanceManagerRights!.Value.HasFlag(InstanceManagerRights.List))
					query = query
						.Where(x => x.InstancePermissionSets.Any(y => y.PermissionSetId == AuthenticationContext.PermissionSet.Id))
						.Where(x => x.InstancePermissionSets.Any(instanceUser =>
							instanceUser.EngineRights != EngineRights.None ||
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
				.Where(x => !x.StoppedAt.HasValue && x.JobCode == JobCode.Move)
				.Include(x => x.StartedBy!)
					.ThenInclude(x => x.CreatedBy)
				.Include(x => x.Instance)
				.ToListAsync(cancellationToken);

			var needsUpdate = false;
			var result = await Paginated<Models.Instance, InstanceResponse>(
				() => ValueTask.FromResult(
					new PaginatableResult<Models.Instance>(
						GetBaseQuery()
							.OrderBy(x => x.Id))),
				async instance =>
				{
					needsUpdate |= ValidateInstanceOnlineStatus(instance);
					instance.MoveJob = moveJobs.FirstOrDefault(x => x.Instance!.Id == instance.Id)?.ToApi();
					await CheckAccessible(instance, cancellationToken);
				},
				page,
				pageSize,
				cancellationToken);

			if (needsUpdate)
				await DatabaseContext.Save(cancellationToken);

			return result;
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.Instance"/>.
		/// </summary>
		/// <param name="id">The instance <see cref="EntityId.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Instance"/> successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(InstanceManagerRights.List | InstanceManagerRights.Read)]
		[ProducesResponseType(typeof(InstanceResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var cantList = !AuthenticationContext.PermissionSet.InstanceManagerRights!.Value.HasFlag(InstanceManagerRights.List);
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

			var instance = await QueryForUser().FirstOrDefaultAsync(cancellationToken);

			if (instance == null)
				return this.Gone();

			if (ValidateInstanceOnlineStatus(instance))
				await DatabaseContext.Save(cancellationToken);

			if (cantList && !instance.InstancePermissionSets.Any(instanceUser => instanceUser.PermissionSetId == AuthenticationContext.PermissionSet.Require(x => x.Id)
				&& (instanceUser.RepositoryRights != RepositoryRights.None ||
				instanceUser.EngineRights != EngineRights.None ||
				instanceUser.ChatBotRights != ChatBotRights.None ||
				instanceUser.ConfigurationRights != ConfigurationRights.None ||
				instanceUser.DreamDaemonRights != DreamDaemonRights.None ||
				instanceUser.DreamMakerRights != DreamMakerRights.None ||
				instanceUser.InstancePermissionSetRights != InstancePermissionSetRights.None)))
				return Forbid();

			var api = instance.ToApi();

			var moveJob = await QueryForUser()
				.SelectMany(x => x.Jobs)
				.Where(x => !x.StoppedAt.HasValue && x.JobCode == JobCode.Move)
				.Include(x => x.StartedBy!)
					.ThenInclude(x => x.CreatedBy)
				.Include(x => x.Instance)
				.FirstOrDefaultAsync(cancellationToken);
			api.MoveJob = moveJob?.ToApi();
			await CheckAccessible(api, cancellationToken);
			return Json(api);
		}

		/// <summary>
		/// Gives the current user full permissions on a given instance <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The instance <see cref="EntityId.Id"/> to give permissions on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Granted permissions successfully.</response>
		[HttpPatch("{id}")]
		[TgsAuthorize(InstanceManagerRights.GrantPermissions)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> GrantPermissions(long id, CancellationToken cancellationToken)
		{
			IQueryable<Models.Instance> BaseQuery() => DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == id && x.SwarmIdentifer == swarmConfiguration.Identifier);

			// ensure the current user has write privilege on the instance
			var usersInstancePermissionSet = await BaseQuery()
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == AuthenticationContext.PermissionSet.Id)
				.FirstOrDefaultAsync(cancellationToken);
			if (usersInstancePermissionSet == default)
			{
				// does the instance actually exist?
				var instanceExists = await BaseQuery()
					.AnyAsync(cancellationToken);

				if (!instanceExists)
					return this.Gone();

				var instanceAdminUser = InstanceAdminPermissionSet(null);
				instanceAdminUser.InstanceId = id;
				DatabaseContext.InstancePermissionSets.Add(instanceAdminUser);
			}
			else
				InstanceAdminPermissionSet(usersInstancePermissionSet);

			await DatabaseContext.Save(cancellationToken);

			return NoContent();
		}

		/// <summary>
		/// Creates a default <see cref="Models.Instance"/> from <paramref name="initialSettings"/>.
		/// </summary>
		/// <param name="initialSettings">The <see cref="InstanceCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="Models.Instance"/> or <see langword="null"/> if ports could not be allocated.</returns>
		async ValueTask<Models.Instance?> CreateDefaultInstance(InstanceCreateRequest initialSettings, CancellationToken cancellationToken)
		{
			var ddPort = await portAllocator.GetAvailablePort(1024, false, cancellationToken);
			if (!ddPort.HasValue)
				return null;

			// try to use the old default if possible
			const ushort DefaultDreamDaemonPort = 1337;
			if (ddPort.Value < DefaultDreamDaemonPort)
				ddPort = await portAllocator.GetAvailablePort(DefaultDreamDaemonPort, false, cancellationToken) ?? ddPort;

			const ushort DefaultApiValidationPort = 1339;
			var dmPort = await portAllocator
				.GetAvailablePort(
					Math.Min((ushort)(ddPort.Value + 1), DefaultApiValidationPort),
					false,
					cancellationToken);
			if (!dmPort.HasValue)
				return null;

			// try to use the old default if possible
			if (dmPort < DefaultApiValidationPort)
				dmPort = await portAllocator.GetAvailablePort(DefaultApiValidationPort, false, cancellationToken) ?? dmPort;

			return new Models.Instance
			{
				ConfigurationType = initialSettings.ConfigurationType ?? ConfigurationType.Disallowed,
				DreamDaemonSettings = new DreamDaemonSettings
				{
					AllowWebClient = false,
					AutoStart = false,
					Port = ddPort,
					OpenDreamTopicPort = 0,
					SecurityLevel = DreamDaemonSecurity.Safe,
					Visibility = DreamDaemonVisibility.Public,
					StartupTimeout = 60,
					HealthCheckSeconds = 60,
					DumpOnHealthCheckRestart = false,
					TopicRequestTimeout = generalConfiguration.ByondTopicTimeout,
					AdditionalParameters = String.Empty,
					StartProfiler = false,
					LogOutput = false,
					MapThreads = 0,
					Minidumps = true,
				},
				DreamMakerSettings = new DreamMakerSettings
				{
					ApiValidationPort = dmPort,
					ApiValidationSecurityLevel = DreamDaemonSecurity.Safe,
					RequireDMApiValidation = true,
					Timeout = TimeSpan.FromHours(1),
					CompilerAdditionalArguments = null,
				},
				Name = initialSettings.Name,
				Online = false,
				Path = initialSettings.Path,
				AutoUpdateInterval = initialSettings.AutoUpdateInterval ?? 0,
				AutoUpdateCron = initialSettings.AutoUpdateCron ?? String.Empty,
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
					CreateGitHubDeployments = false,
					UpdateSubmodules = true,
				},
				InstancePermissionSets = new List<InstancePermissionSet> // give this user full privileges on the instance
				{
					InstanceAdminPermissionSet(null),
				},
				SwarmIdentifer = swarmConfiguration.Identifier,
			};
		}

		/// <summary>
		/// Generate an <see cref="InstancePermissionSet"/> with full rights.
		/// </summary>
		/// <param name="permissionSetToModify">An optional existing <see cref="InstancePermissionSet"/> to update.</param>
		/// <returns><paramref name="permissionSetToModify"/> or a new <see cref="InstancePermissionSet"/> with full rights.</returns>
		InstancePermissionSet InstanceAdminPermissionSet(InstancePermissionSet? permissionSetToModify)
		{
			permissionSetToModify ??= new InstancePermissionSet()
			{
				PermissionSet = AuthenticationContext.PermissionSet,
				PermissionSetId = AuthenticationContext.PermissionSet.Require(x => x.Id),
			};
			permissionSetToModify.EngineRights = RightsHelper.AllRights<EngineRights>();
			permissionSetToModify.ChatBotRights = RightsHelper.AllRights<ChatBotRights>();
			permissionSetToModify.ConfigurationRights = RightsHelper.AllRights<ConfigurationRights>();
			permissionSetToModify.DreamDaemonRights = RightsHelper.AllRights<DreamDaemonRights>();
			permissionSetToModify.DreamMakerRights = RightsHelper.AllRights<DreamMakerRights>();
			permissionSetToModify.RepositoryRights = RightsHelper.AllRights<RepositoryRights>();
			permissionSetToModify.InstancePermissionSetRights = RightsHelper.AllRights<InstancePermissionSetRights>();
			return permissionSetToModify;
		}

		/// <summary>
		/// Normalize a given <paramref name="path"/> for an instance.
		/// </summary>
		/// <param name="path">The path to normalize.</param>
		/// <returns>The normalized <paramref name="path"/>.</returns>
		[return: NotNullIfNotNull(nameof(path))]
		string? NormalizePath(string? path)
		{
			if (path == null)
				return null;

			path = ioManager.ResolvePath(path);
			if (platformIdentifier.IsWindows)
				path = path.ToUpperInvariant().Replace('\\', '/');

			return path;
		}

		/// <summary>
		/// Populate the <see cref="InstanceResponse.Accessible"/> property of a given <paramref name="instanceResponse"/>.
		/// </summary>
		/// <param name="instanceResponse">The <see cref="InstanceResponse"/> to populate.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask CheckAccessible(InstanceResponse instanceResponse, CancellationToken cancellationToken)
		{
			instanceResponse.Accessible = await DatabaseContext
				.InstancePermissionSets
				.AsQueryable()
				.Where(x => x.InstanceId == instanceResponse.Id && x.PermissionSetId == AuthenticationContext.PermissionSet.Id)
				.AnyAsync(cancellationToken);
		}

		/// <summary>
		/// Validates a given <paramref name="instance"/>'s <see cref="Api.Models.Instance.AutoUpdateCron"/> setting.
		/// </summary>
		/// <param name="instance">The <see cref="Api.Models.Instance"/> to validate.</param>
		/// <returns><see langword="null"/> if <paramref name="instance"/> has a valid <see cref="Api.Models.Instance.AutoUpdateCron"/> setting, a <see cref="BadRequestObjectResult"/> otherwise.</returns>
		BadRequestObjectResult? ValidateCronSetting(Api.Models.Instance instance)
		{
			if (!String.IsNullOrWhiteSpace(instance.AutoUpdateCron))
			{
				if ((instance.AutoUpdateInterval.HasValue && instance.AutoUpdateInterval.Value != 0)
				   || (CrontabSchedule.TryParse(
					   instance.AutoUpdateCron,
					   new CrontabSchedule.ParseOptions
					   {
						   IncludingSeconds = true,
					   }) == null))
					return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

				instance.AutoUpdateInterval = 0;
			}
			else
				instance.AutoUpdateCron = String.Empty;

			return null;
		}
	}
}
