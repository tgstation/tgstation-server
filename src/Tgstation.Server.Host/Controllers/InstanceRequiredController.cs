using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see langword="abstract"/> <see cref="ApiController"/> for operations on an <see cref="IInstance"/>.
	/// </summary>
	public abstract class InstanceRequiredController : ApiController
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceRequiredController"/> <see langword="class"/>.
		/// </summary>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		protected InstanceRequiredController(
			IInstanceManager instanceManager,
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<InstanceRequiredController> logger)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  true,
				  true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		protected override async Task<IActionResult> ValidateInstanceRequest(CancellationToken cancellationToken)
		{
			if (ValidateInstanceOnlineStatus(instanceManager, Logger, Instance))
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if (instanceManager.GetInstance(Instance) == null)
				return Conflict(new ErrorMessage(ErrorCode.InstanceOffline));
			return null;
		}

		/// <summary>
		/// Run a given <paramref name="action"/> with the relevant <see cref="IInstance"/>.
		/// </summary>
		/// <param name="action">A <see cref="Func{T, TResult}"/> accepting the <see cref="IInstance"/> and returning a <see cref="Task{TResult}"/> with the <see cref="IActionResult"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> that should be returned.</returns>
		/// <remarks>The context of <paramref name="action"/> should be as small as possinle so as to avoid race conditions.</remarks>
		protected async Task<IActionResult> WithComponentInstance(Func<IInstance, Task<IActionResult>> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			var componentInstance = instanceManager.GetInstance(Instance);
			if (componentInstance == null)
				return Conflict(new ErrorMessage(ErrorCode.InstanceOffline));
			return await action(componentInstance).ConfigureAwait(false);
		}

		/// <summary>
		/// Corrects discrepencies between the <see cref="Api.Models.Instance.Online"/> status of <see cref="IInstance"/>s in the database vs the service.
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="metadata">The <see cref="Models.Instance"/> to check.</param>
		/// <returns><see langword="true"/> if an unsaved DB update was made, <see langword="false"/> otherwise.</returns>
		public static bool ValidateInstanceOnlineStatus(IInstanceManager instanceManager, ILogger logger, Models.Instance metadata)
		{
			if (instanceManager == null)
				throw new ArgumentNullException(nameof(instanceManager));
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			var online = instanceManager.GetInstance(metadata) != null;

			if (metadata.Online.Value == online)
				return false;

			const string OfflineWord = "offline";
			const string OnlineWord = "online";

			logger.LogWarning(
				"Instance {0} is says it's {1} in the database, but it is actually {2} in the service. Updating the database to reflect this...",
				online ? OfflineWord : OnlineWord,
				online ? OnlineWord : OfflineWord);

			metadata.Online = online;
			return true;
		}
	}
}
