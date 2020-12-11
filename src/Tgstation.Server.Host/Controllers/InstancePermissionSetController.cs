using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="InstancePermissionSet"/>s.
	/// </summary>
	[Route(Routes.InstancePermissionSet)]
	public sealed class InstancePermissionSetController : InstanceRequiredController
	{
		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public InstancePermissionSetController(
			IInstanceManager instanceManager,
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<InstancePermissionSetController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{ }

		/// <summary>
		/// Create an <see cref="Api.Models.InstancePermissionSet"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.InstancePermissionSet"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="201"><see cref="Api.Models.InstancePermissionSet"/> created successfully.</response>
		[HttpPut]
		[TgsAuthorize(InstancePermissionSetRights.Create)]
		[ProducesResponseType(typeof(Api.Models.InstancePermissionSet), 201)]
		public async Task<IActionResult> Create([FromBody] Api.Models.InstancePermissionSet model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var userCanonicalName = await DatabaseContext
				.Users
				.AsQueryable()
				.Where(x => x.Id == model.PermissionSetId)
				.Select(x => x.CanonicalName)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);

			if (userCanonicalName == default)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			if (userCanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			var dbUser = new Models.InstancePermissionSet
			{
				ByondRights = RightsHelper.Clamp(model.ByondRights ?? ByondRights.None),
				ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? ChatBotRights.None),
				ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? ConfigurationRights.None),
				DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? DreamDaemonRights.None),
				DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? DreamMakerRights.None),
				RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? RepositoryRights.None),
				InstancePermissionSetRights = RightsHelper.Clamp(model.InstancePermissionSetRights ?? InstancePermissionSetRights.None),
				PermissionSetId = model.PermissionSetId,
				InstanceId = Instance.Id
			};

			DatabaseContext.InstancePermissionSets.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			return Created(dbUser.ToApi());
		}

		/// <summary>
		/// Update the permissions for an <see cref="Api.Models.InstancePermissionSet"/>.
		/// </summary>
		/// <param name="model">The updated <see cref="Api.Models.InstancePermissionSet"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="Api.Models.InstancePermissionSet"/> updated successfully.</response>
		/// <response code="410">The requested <see cref="Api.Models.InstancePermissionSet"/> does not currently exist.</response>
		[HttpPost]
		[TgsAuthorize(InstancePermissionSetRights.Write)]
		[ProducesResponseType(typeof(Api.Models.InstancePermissionSet), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] Api.Models.InstancePermissionSet model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var originalPermissionSet = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == model.PermissionSetId)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (originalPermissionSet == null)
				return Gone();

			originalPermissionSet.ByondRights = RightsHelper.Clamp(model.ByondRights ?? originalPermissionSet.ByondRights.Value);
			originalPermissionSet.RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? originalPermissionSet.RepositoryRights.Value);
			originalPermissionSet.InstancePermissionSetRights = RightsHelper.Clamp(model.InstancePermissionSetRights ?? originalPermissionSet.InstancePermissionSetRights.Value);
			originalPermissionSet.ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? originalPermissionSet.ChatBotRights.Value);
			originalPermissionSet.ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? originalPermissionSet.ConfigurationRights.Value);
			originalPermissionSet.DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? originalPermissionSet.DreamDaemonRights.Value);
			originalPermissionSet.DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? originalPermissionSet.DreamMakerRights.Value);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			var showFullPermissionSet = originalPermissionSet.PermissionSetId == AuthenticationContext.PermissionSet.Id.Value
				|| (AuthenticationContext.GetRight(RightsType.InstancePermissionSet) & (ulong)InstancePermissionSetRights.Read) != 0;
			return Json(
				showFullPermissionSet
					? originalPermissionSet.ToApi()
					: new Api.Models.InstancePermissionSet
					{
						PermissionSetId = originalPermissionSet.PermissionSetId
					});
		}
#pragma warning restore CA1506
		/// <summary>
		/// Read the active <see cref="Api.Models.InstancePermissionSet"/>.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="Api.Models.InstancePermissionSet"/> retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.InstancePermissionSet), 200)]
		public IActionResult Read() => Json(AuthenticationContext.InstancePermissionSet.ToApi());

		/// <summary>
		/// Lists <see cref="Api.Models.InstancePermissionSet"/>s for the instance.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.InstancePermissionSet"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(InstancePermissionSetRights.Read)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.InstancePermissionSet>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var permissionSets = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);
			return Json(permissionSets.Select(x => x.ToApi()));
		}

		/// <summary>
		/// Gets a specific <see cref="Api.Models.InstancePermissionSet"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.InstancePermissionSet.PermissionSetId"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieve <see cref="Api.Models.InstancePermissionSet"/> successfully.</response>
		/// <response code="410">The requested <see cref="Api.Models.InstancePermissionSet"/> does not currently exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(InstancePermissionSetRights.Read)]
		[ProducesResponseType(typeof(Api.Models.InstancePermissionSet), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			// this functions as userId
			var permissionSet = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == id)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (permissionSet == default)
				return Gone();
			return Json(permissionSet.ToApi());
		}

		/// <summary>
		/// Delete an <see cref="Api.Models.InstancePermissionSet"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.InstancePermissionSet.PermissionSetId"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Target <see cref="Api.Models.InstancePermissionSet"/> deleted.</response>
		/// <response code="410">Target <see cref="Api.Models.InstancePermissionSet"/> or no longer exists.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(InstancePermissionSetRights.Write)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var numDeleted = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstancePermissionSets)
				.Where(x => x.PermissionSetId == id)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(false);
			return numDeleted > 0 ? (IActionResult)NoContent() : Gone();
		}
	}
}
