using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Controllers.Transformers;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="UserGroupResponse"/>s.
	/// </summary>
	[Route(Routes.UserGroup)]
	[Authorize]
	public class UserGroupController : ApiController
	{
		/// <summary>
		/// The <see cref="IUserGroupAuthority"/> for the <see cref="UserGroupController"/>.
		/// </summary>
		readonly IRestAuthorityInvoker<IUserGroupAuthority> userGroupAuthority;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="userGroupAuthority">The value of <see cref="userGroupAuthority"/>.</param>
		public UserGroupController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IApiHeadersProvider apiHeaders,
			ILogger<UserGroupController> logger,
			IRestAuthorityInvoker<IUserGroupAuthority> userGroupAuthority)
			: base(
				databaseContext,
				authenticationContext,
				apiHeaders,
				logger,
				true)
		{
			this.userGroupAuthority = userGroupAuthority ?? throw new ArgumentNullException(nameof(userGroupAuthority));
		}

		/// <summary>
		/// Transform a <see cref="Api.Models.PermissionSet"/> into a <see cref="PermissionSet"/>.
		/// </summary>
		/// <param name="permissionSet">The <see cref="Api.Models.PermissionSet"/> to transform.</param>
		/// <returns>The transformed <paramref name="permissionSet"/>.</returns>
		static Models.PermissionSet? TransformApiPermissionSet(Api.Models.PermissionSet? permissionSet)
			=> permissionSet != null
				? new Models.PermissionSet
				{
					InstanceManagerRights = permissionSet?.InstanceManagerRights,
					AdministrationRights = permissionSet?.AdministrationRights,
				}
				: null;

		/// <summary>
		/// Create a new <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserGroupCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="UserGroup"/> created successfully.</response>
		[HttpPut]
		[ProducesResponseType(typeof(UserGroupResponse), 201)]
		public async ValueTask<IActionResult> Create([FromBody] UserGroupCreateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.Name == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			return await userGroupAuthority.InvokeTransformable<UserGroup, UserGroupResponse>(
				this,
				authority => authority.Create(
					model.Name,
					TransformApiPermissionSet(model.PermissionSet),
					cancellationToken));
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
		[ProducesResponseType(typeof(UserGroupResponse), 200)]
		public ValueTask<IActionResult> Update([FromBody] UserGroupUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			return userGroupAuthority.InvokeTransformable<UserGroup, UserGroupResponse>(
				this,
				authority => authority.Update(
					model.Require(x => x.Id),
					model.Name,
					TransformApiPermissionSet(model.PermissionSet),
					cancellationToken));
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
		[ProducesResponseType(typeof(UserGroupResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
			=> userGroupAuthority.InvokeTransformable<UserGroup, UserGroupResponse, UserGroupResponseTransformer>(
				this,
				authority => authority.GetId<UserGroupResponse>(id, cancellationToken));

		/// <summary>
		/// Lists all <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="UserGroup"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[ProducesResponseType(typeof(PaginatedResponse<UserGroupResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<UserGroup, UserGroupResponse>(
				async () =>
				{
					var queryable = await userGroupAuthority
						.InvokeQueryable(authority => authority.Queryable(true));
					if (queryable == null)
						return null;

					return new PaginatableResult<UserGroup>(queryable.OrderBy(x => x.Id));
				},
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
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
#pragma warning disable API1001 // The response type is RIGHT THERE ^^^
			=> userGroupAuthority.Invoke(this, authority => authority.DeleteEmpty(id, cancellationToken));
#pragma warning restore API1001
	}
}
