using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Security;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="UserGroup"/>s.
	/// </summary>
	[Route(Routes.UserGroup)]
	public class UserGroupController : ApiController
	{
		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserGroupController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupController"/> <see langword="clas"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		public UserGroupController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			ILogger<UserGroupController> logger)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  true)
		{
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Create a new <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserGroup"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="UserGroup"/> created successfully.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserGroup), 201)]
		public async Task<IActionResult> Create([FromBody] UserGroup model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Name == null)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			var totalGroups = await DatabaseContext
				.Groups
				.AsQueryable()
				.CountAsync(cancellationToken)
				.ConfigureAwait(false);
			if (totalGroups >= generalConfiguration.UserGroupLimit)
				return Conflict(new ErrorMessage(ErrorCode.UserGroupLimitReached));

			var permissionSet = new Models.PermissionSet
			{
				AdministrationRights = model.PermissionSet?.AdministrationRights ?? AdministrationRights.None,
				InstanceManagerRights = model.PermissionSet?.InstanceManagerRights ?? InstanceManagerRights.None
			};

			var dbGroup = new Models.UserGroup
			{
				Name = model.Name,
				PermissionSet = permissionSet,
			};

			DatabaseContext.Groups.Add(dbGroup);
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			Logger.LogInformation("Created new user group {0} ({1})", dbGroup.Name, dbGroup.Id);

			return Created(dbGroup.ToApi(true));
		}

		/// <summary>
		/// Update a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserGroup"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="UserGroup"/> updated successfully.</response>
		/// <response code="410">The requested <see cref="UserGroup"/> does not currently exist.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserGroup), 200)]
		public async Task<IActionResult> Update([FromBody] UserGroup model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			// For my sanity, I'm not allowing user management here
			// Use the UserController for that
			if (model.Users != null)
				return BadRequest(new ErrorMessage(ErrorCode.UserGroupControllerCantEditMembers));

			var currentGroup = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == model.Id)
				.Include(x => x.PermissionSet)
				.Include(x => x.Users)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);

			if (currentGroup == default)
				return Gone();

			if (model.PermissionSet != null)
			{
				currentGroup.PermissionSet.AdministrationRights = model.PermissionSet.AdministrationRights ?? currentGroup.PermissionSet.AdministrationRights;
				currentGroup.PermissionSet.InstanceManagerRights = model.PermissionSet.InstanceManagerRights ?? currentGroup.PermissionSet.InstanceManagerRights;
			}

			currentGroup.Name = model.Name ?? currentGroup.Name;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if (!AuthenticationContext.PermissionSet.AdministrationRights.Value.HasFlag(AdministrationRights.ReadUsers))
				return Json(new UserGroup
				{
					Id = currentGroup.Id
				});

			return Json(currentGroup.ToApi(true));
		}

		/// <summary>
		/// Gets a specific <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="UserGroup"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieve <see cref="UserGroup"/> successfully.</response>
		/// <response code="410">The requested <see cref="UserGroup"/> does not currently exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(UserGroup), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			// this functions as userId
			var group = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.Include(x => x.Users)
				.Include(x => x.PermissionSet)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (group == default)
				return Gone();
			return Json(group.ToApi(true));
		}

		/// <summary>
		/// Lists all <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="UserGroup"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(Paginated<UserGroup>), 200)]
		public Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Models.UserGroup, UserGroup>(
				() => Task.FromResult(
					new PaginatableResult<Models.UserGroup>(
						DatabaseContext
							.Groups
							.AsQueryable()
							.Include(x => x.Users)
							.Include(x => x.PermissionSet))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Delete an <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="UserGroup"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204"><see cref="UserGroup"/> was deleted.</response>
		/// <response code="409">The <see cref="UserGroup"/> is not empty.</response>
		/// <response code="410">The <see cref="UserGroup"/> didn't exist.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessage), 409)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var numDeleted = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id && x.Users.Count == 0)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(false);

			if (numDeleted > 0)
				return NoContent();

			// find out how we failed
			var groupExists = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.AnyAsync(cancellationToken)
				.ConfigureAwait(false);

			return groupExists
				? Conflict(new ErrorMessage(ErrorCode.UserGroupNotEmpty))
				: Gone();
		}
	}
}
