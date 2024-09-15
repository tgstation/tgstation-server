using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for managing <see cref="User"/>s.
	/// </summary>
	public interface IUserAuthority : IAuthority
	{
		/// <summary>
		/// Gets the currently authenticated user.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize]
		ValueTask<AuthorityResponse<User>> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="User"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="allowSystemUser">If the <see cref="User.TgsSystemUserName"/> may be returned.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		ValueTask<AuthorityResponse<User>> GetId(long id, bool includeJoins, bool allowSystemUser, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="OAuthConnection"/>s for the <see cref="User"/> with a given <paramref name="userId"/>.
		/// </summary>
		/// <param name="userId">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="global::System.Array"/> of <see cref="GraphQL.Types.OAuthConnection"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<AuthorityResponse<GraphQL.Types.OAuthConnection[]>> OAuthConnections(long userId, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of <see cref="User"/>s.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		IQueryable<User> Queryable(bool includeJoins);

		/// <summary>
		/// Creates a <see cref="User"/>.
		/// </summary>
		/// <param name="createRequest">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in am <see cref="AuthorityResponse{TResult}"/> for the created <see cref="User"/>.</returns>
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		ValueTask<AuthorityResponse<User>> Create(UserCreateRequest createRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Updates a <see cref="User"/>.
		/// </summary>
		/// <param name="updateRequest">The <see cref="UserUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in am <see cref="AuthorityResponse{TResult}"/> for the created <see cref="User"/>.</returns>
		[TgsAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword | AdministrationRights.EditOwnOAuthConnections)]
		ValueTask<AuthorityResponse<User>> Update(UserUpdateRequest updateRequest, CancellationToken cancellationToken);
	}
}
