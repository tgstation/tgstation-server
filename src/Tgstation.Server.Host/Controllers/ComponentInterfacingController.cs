using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for operations on <see cref="IInstanceCore"/>s.
	/// </summary>
	public abstract class ComponentInterfacingController : ApiController
	{
		/// <summary>
		/// Access the <see cref="IInstanceOperations"/> instance.
		/// </summary>
		public IInstanceOperations InstanceOperations => instanceManager;

		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ComponentInterfacingController"/>.
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// If the <see cref="Api.ApiHeaders.InstanceId"/> header should be checked and used to perform validation for every request.
		/// </summary>
		readonly bool useInstanceRequestHeader;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComponentInterfacingController"/> class.
		/// </summary>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		/// <param name="useInstanceRequestHeader">The value of <see cref="useInstanceRequestHeader"/>.</param>
		protected ComponentInterfacingController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<ComponentInterfacingController> logger,
			IInstanceManager instanceManager,
			IApiHeadersProvider apiHeaders,
			bool useInstanceRequestHeader)
			: base(
				  databaseContext,
				  authenticationContext,
				  apiHeaders,
				  logger,
				  true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.useInstanceRequestHeader = useInstanceRequestHeader;
		}

		/// <inheritdoc />
		protected override async ValueTask<IActionResult?> ValidateRequest(CancellationToken cancellationToken)
		{
			if (!useInstanceRequestHeader)
				return null;

			if (!ApiHeaders!.InstanceId.HasValue)
				return BadRequest(new ErrorMessageResponse(ErrorCode.InstanceHeaderRequired));

			if (AuthenticationContext.InstancePermissionSet == null)
				return Forbid();

			var instance = Instance!;
			if (ValidateInstanceOnlineStatus(instance))
				await DatabaseContext.Save(cancellationToken);

			using var instanceReferenceCheck = instanceManager.GetInstanceReference(instance);
			if (instanceReferenceCheck == null)
				return Conflict(new ErrorMessageResponse(ErrorCode.InstanceOffline));

			return null;
		}

		/// <summary>
		/// Corrects discrepencies between the <see cref="Api.Models.Instance.Online"/> status of <see cref="IInstance"/>s in the database vs the service.
		/// </summary>
		/// <param name="metadata">The <see cref="Api.Models.Instance"/> to check.</param>
		/// <returns><see langword="true"/> if an unsaved DB update was made, <see langword="false"/> otherwise.</returns>
		protected bool ValidateInstanceOnlineStatus(Api.Models.Instance metadata)
		{
			ArgumentNullException.ThrowIfNull(metadata);

			bool online;
			using (var instanceReferenceCheck = instanceManager.GetInstanceReference(metadata))
				online = instanceReferenceCheck != null;

			if (metadata.Require(x => x.Online) == online)
				return false;

			const string OfflineWord = "offline";
			const string OnlineWord = "online";

			Logger.LogWarning(
				"Instance {instanceId} is says it's {databaseState} in the database, but it is actually {serviceState} in the service. Updating the database to reflect this...",
				metadata.Id,
				online ? OfflineWord : OnlineWord,
				online ? OnlineWord : OfflineWord);

			metadata.Online = online;
			return true;
		}

		/// <summary>
		/// Run a given <paramref name="action"/> with the relevant <see cref="IInstance"/>.
		/// </summary>
		/// <param name="action">A <see cref="Func{T, TResult}"/> accepting the <see cref="IInstance"/> and returning a <see cref="ValueTask{TResult}"/> with the <see cref="IActionResult"/>.</param>
		/// <param name="instance">The <see cref="Models.Instance"/> to grab. If <see langword="null"/>, <see cref="ApiController.Instance"/> will be used.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> that should be returned.</returns>
		/// <remarks>The context of <paramref name="action"/> should be as small as possible so as to avoid race conditions. This function can return a <see cref="ConflictResult"/> if the requested instance was offline.</remarks>
		protected async ValueTask<IActionResult?> WithComponentInstanceNullable(Func<IInstanceCore, ValueTask<IActionResult?>> action, Models.Instance? instance = null)
		{
			ArgumentNullException.ThrowIfNull(action);

			instance ??= Instance ?? throw new InvalidOperationException("ComponentInterfacingController has no Instance!");

			using var instanceReference = instanceManager.GetInstanceReference(instance);
			using (LogContext.PushProperty(SerilogContextHelper.InstanceReferenceContextProperty, instanceReference?.Uid))
			{
				if (instanceReference == null)
					return Conflict(new ErrorMessageResponse(ErrorCode.InstanceOffline));
				return await action(instanceReference);
			}
		}

		/// <summary>
		/// Run a given <paramref name="action"/> with the relevant <see cref="IInstance"/>.
		/// </summary>
		/// <param name="action">A <see cref="Func{T, TResult}"/> accepting the <see cref="IInstance"/> and returning a <see cref="ValueTask{TResult}"/> with the <see cref="IActionResult"/>.</param>
		/// <param name="instance">The <see cref="Models.Instance"/> to grab. If <see langword="null"/>, <see cref="ApiController.Instance"/> will be used.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> that should be returned.</returns>
		/// <remarks>The context of <paramref name="action"/> should be as small as possible so as to avoid race conditions. This function can return a <see cref="ConflictResult"/> if the requested instance was offline.</remarks>
		protected async ValueTask<IActionResult> WithComponentInstance(Func<IInstanceCore, ValueTask<IActionResult>> action, Models.Instance? instance = null)
			=> (await WithComponentInstanceNullable(async core => await action(core), instance))!;
	}
}
