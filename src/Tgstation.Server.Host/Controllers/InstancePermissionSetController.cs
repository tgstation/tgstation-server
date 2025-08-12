using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="InstancePermissionSet"/>s.
	/// </summary>
	[Route(Routes.InstancePermissionSet)]
	public sealed class InstancePermissionSetController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IPermissionsUpdateNotifyee"/> for the <see cref="InstancePermissionSetController"/>.
		/// </summary>
		readonly IPermissionsUpdateNotifyee permissionsUpdateNotifyee;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstancePermissionSetController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="permissionsUpdateNotifyee">The value of <see cref="permissionsUpdateNotifyee"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public InstancePermissionSetController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<InstancePermissionSetController> logger,
			IInstanceManager instanceManager,
			IPermissionsUpdateNotifyee permissionsUpdateNotifyee,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
			this.permissionsUpdateNotifyee = permissionsUpdateNotifyee ?? throw new ArgumentNullException(nameof(permissionsUpdateNotifyee));
		}

		/// <summary>
		/// Create an <see cref="InstancePermissionSet"/>.
		/// </summary>
		/// <param name="model">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="201"><see cref="InstancePermissionSet"/> created successfully.</response>
		/// <response code="410">The <see cref="Api.Models.PermissionSet"/> does not exist.</response>
		[HttpPost(Routes.Create)]
		[TgsAuthorize(InstancePermissionSetRights.Create)]
		[ProducesResponseType(typeof(InstancePermissionSetResponse), 201)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1506
		public async ValueTask<IActionResult> Create([FromBody] InstancePermissionSetRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			var existingPermissionSet = await DatabaseContext
				.PermissionSets
				.AsQueryable()
				.Where(x => x.Id == model.PermissionSetId)
				.Select(x => new Models.PermissionSet
				{
					Id = x.Id,
					UserId = x.UserId,
				})
				.FirstOrDefaultAsync(cancellationToken);

			if (existingPermissionSet == default)
				return this.Gone();

			if (existingPermissionSet.UserId.HasValue)
			{
				var userCanonicalName = await DatabaseContext
					.Users
					.AsQueryable()
					.Where(x => x.Id == existingPermissionSet.UserId.Value)
					.Select(x => x.CanonicalName)
					.FirstAsync(cancellationToken);

				if (userCanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
					return Forbid();
			}

			var dbUser = new InstancePermissionSet
			{
				EngineRights = RightsHelper.Clamp(model.EngineRights ?? EngineRights.None),
				ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? ChatBotRights.None),
				ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? ConfigurationRights.None),
				DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? DreamDaemonRights.None),
				DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? DreamMakerRights.None),
				RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? RepositoryRights.None),
				InstancePermissionSetRights = RightsHelper.Clamp(model.InstancePermissionSetRights ?? InstancePermissionSetRights.None),
				PermissionSetId = model.PermissionSetId,
				InstanceId = Instance.Require(x => x.Id),
			};

			DatabaseContext.InstancePermissionSets.Add(dbUser);

			await DatabaseContext.Save(cancellationToken);

			// needs to be set for next call
			dbUser.PermissionSet = existingPermissionSet;
			await permissionsUpdateNotifyee.InstancePermissionSetCreated(dbUser, cancellationToken);
			return this.Created(dbUser.ToApi());
		}
#pragma warning restore CA1506

		/// <summary>
		/// Update the permissions for an <see cref="InstancePermissionSet"/>.
		/// </summary>
		/// <param name="model">The <see cref="InstancePermissionSetRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="InstancePermissionSet"/> updated successfully.</response>
		/// <response code="410">The requested <see cref="InstancePermissionSet"/> does not currently exist.</response>
		[HttpPost(Routes.Update)]
		[TgsAuthorize(InstancePermissionSetRights.Write)]
		[ProducesResponseType(typeof(InstancePermissionSetResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<IActionResult> Update([FromBody] InstancePermissionSetRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			var originalPermissionSet = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == model.PermissionSetId)
				.FirstOrDefaultAsync(cancellationToken);
			if (originalPermissionSet == null)
				return this.Gone();

			originalPermissionSet.EngineRights = RightsHelper.Clamp(model.EngineRights ?? originalPermissionSet.EngineRights!.Value);
			originalPermissionSet.RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? originalPermissionSet.RepositoryRights!.Value);
			originalPermissionSet.InstancePermissionSetRights = RightsHelper.Clamp(model.InstancePermissionSetRights ?? originalPermissionSet.InstancePermissionSetRights!.Value);
			originalPermissionSet.ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? originalPermissionSet.ChatBotRights!.Value);
			originalPermissionSet.ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? originalPermissionSet.ConfigurationRights!.Value);
			originalPermissionSet.DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? originalPermissionSet.DreamDaemonRights!.Value);
			originalPermissionSet.DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? originalPermissionSet.DreamMakerRights!.Value);

			await DatabaseContext.Save(cancellationToken);
			var showFullPermissionSet = originalPermissionSet.PermissionSetId == AuthenticationContext.PermissionSet.Require(x => x.Id)
				|| (AuthenticationContext.GetRight(RightsType.InstancePermissionSet) & (ulong)InstancePermissionSetRights.Read) != 0;
			return Json(
				showFullPermissionSet
					? originalPermissionSet.ToApi()
					: new InstancePermissionSetResponse
					{
						PermissionSetId = originalPermissionSet.PermissionSetId,
					});
		}
#pragma warning restore CA1506
		/// <summary>
		/// Read the active <see cref="InstancePermissionSet"/>.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="InstancePermissionSet"/> retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(InstancePermissionSetResponse), 200)]
		public IActionResult Read() => Json(InstancePermissionSet.ToApi());

		/// <summary>
		/// Lists <see cref="InstancePermissionSet"/>s for the instance.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="InstancePermissionSet"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(InstancePermissionSetRights.Read)]
		[ProducesResponseType(typeof(PaginatedResponse<InstancePermissionSetResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<InstancePermissionSet, InstancePermissionSetResponse>(
				() => ValueTask.FromResult<PaginatableResult<InstancePermissionSet>?>(
					new PaginatableResult<InstancePermissionSet>(
						DatabaseContext
							.Instances
							.AsQueryable()
							.Where(x => x.Id == Instance.Id)
							.SelectMany(x => x.InstancePermissionSets)
							.OrderBy(x => x.PermissionSetId))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Gets a specific <see cref="Api.Models.Internal.InstancePermissionSet"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.InstancePermissionSet.PermissionSetId"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieve <see cref="Api.Models.Internal.InstancePermissionSet"/> successfully.</response>
		/// <response code="410">The requested <see cref="Api.Models.Internal.InstancePermissionSet"/> does not currently exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(InstancePermissionSetRights.Read)]
		[ProducesResponseType(typeof(InstancePermissionSetResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			// this functions as userId
			var permissionSet = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == id)
				.FirstOrDefaultAsync(cancellationToken);
			if (permissionSet == default)
				return this.Gone();
			return Json(permissionSet.ToApi());
		}

		/// <summary>
		/// Delete an <see cref="InstancePermissionSet"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.InstancePermissionSet.PermissionSetId"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Target <see cref="InstancePermissionSet"/> deleted.</response>
		/// <response code="410">Target <see cref="InstancePermissionSet"/> or no longer exists.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(InstancePermissionSetRights.Write)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var numDeleted = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == id)
				.ExecuteDeleteAsync(cancellationToken);

			return numDeleted > 0 ? NoContent() : this.Gone();
		}
	}
}
