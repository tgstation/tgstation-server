using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="User"/>s.
	/// </summary>
	[Route(Routes.User)]
	public sealed class UserController : ApiController
	{
		/// <summary>
		/// The <see cref="IRestAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.
		/// </summary>
		readonly IRestAuthorityInvoker<IUserAuthority> userAuthority;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="userAuthority">The value of <see cref="userAuthority"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		public UserController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IRestAuthorityInvoker<IUserAuthority> userAuthority,
			ILogger<UserController> logger,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  apiHeaders,
				  logger,
				  true)
		{
			this.userAuthority = userAuthority ?? throw new ArgumentNullException(nameof(userAuthority));
		}

		/// <summary>
		/// Create a new <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="User"/> created successfully.</response>
		/// <response code="410">The requested system identifier could not be found.</response>
		[HttpPut]
		[TgsRestAuthorize<IUserAuthority>(nameof(IUserAuthority.Create))]
		[ProducesResponseType(typeof(UserResponse), 201)]
		public ValueTask<IActionResult> Create([FromBody] UserCreateRequest model, CancellationToken cancellationToken)
			=> userAuthority.InvokeTransformable<User, UserResponse>(this, authority => authority.Create(model, null, cancellationToken));

		/// <summary>
		/// Update a <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="User"/> updated successfully.</response>
		/// <response code="200"><see cref="User"/> updated successfully. Not returned due to lack of permissions.</response>
		/// <response code="404">Requested <see cref="EntityId.Id"/> does not exist.</response>
		/// <response code="410">Requested <see cref="Api.Models.Internal.UserApiBase.Group"/> does not exist.</response>
		[HttpPost]
		[TgsRestAuthorize<IUserAuthority>(nameof(IUserAuthority.Update))]
		[ProducesResponseType(typeof(UserResponse), 200)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public ValueTask<IActionResult> Update([FromBody] UserUpdateRequest model, CancellationToken cancellationToken)
			=> userAuthority.InvokeTransformable<User, UserResponse>(this, authority => authority.Update(model, cancellationToken));

		/// <summary>
		/// Get information about the current <see cref="User"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		[HttpGet]
		[TgsRestAuthorize<IUserAuthority>(nameof(IUserAuthority.Read))]
		[ProducesResponseType(typeof(UserResponse), 200)]
		public ValueTask<IActionResult> Read(CancellationToken cancellationToken)
			=> userAuthority.InvokeTransformable<User, UserResponse>(this, authority => authority.Read(cancellationToken));

		/// <summary>
		/// List all <see cref="User"/>s in the server.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved <see cref="User"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsRestAuthorize<IUserAuthority>(nameof(IUserAuthority.Queryable))]
		[ProducesResponseType(typeof(PaginatedResponse<UserResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<User, UserResponse>(
				() => ValueTask.FromResult(
					new PaginatableResult<User>(
						userAuthority.InvokeQueryable(
							authority => authority.Queryable(true))
							.OrderBy(x => x.Id))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Get a specific <see cref="User"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		/// <response code="404">The <see cref="User"/> does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(UserResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			if (id == AuthenticationContext.User.Id)
				return await Read(cancellationToken);

			if (!((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid();

			return await userAuthority.InvokeTransformable<User, UserResponse>(
				this,
				authority => authority.GetId(id, true, false, cancellationToken));
		}
	}
}
