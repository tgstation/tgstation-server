using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Serilog.Context;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// <see cref="AuthorityBase"/> for <see cref="IAuthority"/>s that need to access <see cref="IInstanceCore"/>s.
	/// </summary>
	abstract class ComponentInterfacingAuthorityBase : AuthorityBase
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ComponentInterfacingAuthorityBase"/>.
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComponentInterfacingAuthorityBase"/> class.
		/// </summary>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="databaseContext">The <see cref="AuthorityBase.DatabaseContext"/>.</param>
		/// <param name="logger">The <see cref="AuthorityBase.Logger"/>.</param>
		protected ComponentInterfacingAuthorityBase(
			IInstanceManager instanceManager,
			IDatabaseContext databaseContext,
			ILogger<ComponentInterfacingAuthorityBase> logger)
			: base(databaseContext, logger)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <summary>
		/// Run a given <paramref name="action"/> with the relevant <see cref="IInstance"/>.
		/// </summary>
		/// <typeparam name="TResult">The type of result the returned <see cref="AuthorityResponse{TResult}"/> uses.</typeparam>
		/// <param name="action">A <see cref="Func{T, TResult}"/> accepting the <see cref="IInstance"/> and returning a <see cref="ValueTask{TResult}"/> with the <see cref="IActionResult"/>.</param>
		/// <param name="instanceId">The <see cref="EntityId.Id"/> of <see cref="Models.Instance"/> to grab.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/> that should be returned.</returns>
		/// <remarks>The context of <paramref name="action"/> should be as small as possible so as to avoid race conditions. This function can return a <see cref="ConflictResult"/> if the requested instance was offline.</remarks>
		protected async ValueTask<AuthorityResponse<TResult>> WithComponentInstance<TResult>(Func<IInstanceCore, ValueTask<AuthorityResponse<TResult>>> action, long instanceId)
		{
			using var instanceReference = instanceManager.GetInstanceReference(instanceId);
			using (LogContext.PushProperty(SerilogContextHelper.InstanceReferenceContextProperty, instanceReference?.Uid))
			{
				if (instanceReference == null)
					return Conflict<TResult>(ErrorCode.InstanceOffline);
				return await action(instanceReference);
			}
		}
	}
}
