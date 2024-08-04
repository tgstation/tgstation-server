using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="UserGroupResponse"/>s.
	/// </summary>
	[Route(Routes.UserGroup)]
	public class UserGroupController : ApiController
	{
		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserGroupController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		public UserGroupController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			ILogger<UserGroupController> logger,
			IApiHeadersProvider apiHeaders)
			: base(
				databaseContext,
				authenticationContext,
				apiHeaders,
				logger,
				true)
		{
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Create a new <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserGroupCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="UserGroup"/> created successfully.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserGroupResponse), 201)]
		public async ValueTask<IActionResult> Create([FromBody] UserGroupCreateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.Name == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var totalGroups = await DatabaseContext
				.Groups
				.AsQueryable()
				.CountAsync(cancellationToken);
			if (totalGroups >= generalConfiguration.UserGroupLimit)
				return Conflict(new ErrorMessageResponse(ErrorCode.UserGroupLimitReached));

			var permissionSet = new Models.PermissionSet
			{
				AdministrationRights = model.PermissionSet?.AdministrationRights ?? AdministrationRights.None,
				InstanceManagerRights = model.PermissionSet?.InstanceManagerRights ?? InstanceManagerRights.None,
			};

			var dbGroup = new UserGroup
			{
				Name = model.Name,
				PermissionSet = permissionSet,
			};

			DatabaseContext.Groups.Add(dbGroup);
			await DatabaseContext.Save(cancellationToken);
			Logger.LogInformation("Created new user group {groupName} ({groupId})", dbGroup.Name, dbGroup.Id);

			return Created(dbGroup.ToApi(true));
		}

		/// <summary>
		/// Update a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserGroupUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="UserGroup"/> updated successfully.</response>
		/// <response code="410">The requested <see cref="UserGroup"/> does not currently exist.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserGroupResponse), 200)]
		public async ValueTask<IActionResult> Update([FromBody] UserGroupUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			var currentGroup = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == model.Id)
				.Include(x => x.PermissionSet)
				.Include(x => x.Users)
				.FirstOrDefaultAsync(cancellationToken);

			if (currentGroup == default)
				return this.Gone();

			if (model.PermissionSet != null)
			{
				currentGroup.PermissionSet!.AdministrationRights = model.PermissionSet.AdministrationRights ?? currentGroup.PermissionSet.AdministrationRights;
				currentGroup.PermissionSet.InstanceManagerRights = model.PermissionSet.InstanceManagerRights ?? currentGroup.PermissionSet.InstanceManagerRights;
			}

			currentGroup.Name = model.Name ?? currentGroup.Name;

			await DatabaseContext.Save(cancellationToken);

			if (!AuthenticationContext.PermissionSet.AdministrationRights!.Value.HasFlag(AdministrationRights.ReadUsers))
				return Json(new UserGroupResponse
				{
					Id = currentGroup.Id,
				});

			return Json(currentGroup.ToApi(true));
		}

		/// <summary>
		/// Gets a specific <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="UserGroupResponse"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieve <see cref="UserGroup"/> successfully.</response>
		/// <response code="410">The requested <see cref="UserGroup"/> does not currently exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(UserGroupResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			// this functions as userId
			var group = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.Include(x => x.Users)
				.Include(x => x.PermissionSet)
				.FirstOrDefaultAsync(cancellationToken);
			if (group == default)
				return this.Gone();
			return Json(group.ToApi(true));
		}

		/// <summary>
		/// Lists all <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="UserGroup"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(PaginatedResponse<UserGroupResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<UserGroup, UserGroupResponse>(
				() => ValueTask.FromResult(
					new PaginatableResult<UserGroup>(
						DatabaseContext
							.Groups
							.AsQueryable()
							.Include(x => x.Users)
							.Include(x => x.PermissionSet)
							.OrderBy(x => x.Id))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Delete a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="UserGroup"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204"><see cref="UserGroup"/> was deleted.</response>
		/// <response code="409">The <see cref="UserGroup"/> is not empty.</response>
		/// <response code="410">The <see cref="UserGroup"/> didn't exist.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var numDeleted = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id && x.Users!.Count == 0)
				.ExecuteDeleteAsync(cancellationToken);

			if (numDeleted > 0)
				return NoContent();

			// find out how we failed
			var groupExists = await DatabaseContext
				.Groups
				.AsQueryable()
				.Where(x => x.Id == id)
				.AnyAsync(cancellationToken);

			return groupExists
				? Conflict(new ErrorMessageResponse(ErrorCode.UserGroupNotEmpty))
				: this.Gone();
		}
	}
}
