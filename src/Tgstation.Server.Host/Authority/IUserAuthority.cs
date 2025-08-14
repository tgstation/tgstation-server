using System.Linq;
using System.Threading;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;

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
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<User>> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="User"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="allowSystemUser">If the <see cref="User.TgsSystemUserName"/> may be returned.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<User>> GetId(long id, bool includeJoins, bool allowSystemUser, CancellationToken cancellationToken);

		RequirementsGated<Projectable<User, TResult>> GetId<TResult>(long id, bool allowSystemUser, CancellationToken cancellationToken)
			where TResult : class;

		/// <summary>
		/// Gets the <see cref="GraphQL.Types.OAuth.OAuthConnection"/>s for the <see cref="User"/> with a given <paramref name="userId"/>.
		/// </summary>
		/// <param name="userId">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="global::System.Array"/> of <see cref="GraphQL.Types.OAuth.OAuthConnection"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<GraphQL.Types.OAuth.OAuthConnection[]>> OAuthConnections(long userId, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="GraphQL.Types.OAuth.OidcConnection"/>s for the <see cref="User"/> with a given <paramref name="userId"/>.
		/// </summary>
		/// <param name="userId">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="global::System.Array"/> of <see cref="GraphQL.Types.OAuth.OidcConnection"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<GraphQL.Types.OAuth.OidcConnection[]>> OidcConnections(long userId, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="IQueryable{T}"/> of <see cref="User"/>s.</returns>
		RequirementsGated<IQueryable<User>> Queryable(bool includeJoins);

		/// <summary>
		/// Creates a <see cref="User"/>.
		/// </summary>
		/// <param name="createRequest">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="needZeroLengthPasswordWithOAuthConnections">If a zero-length <see cref="UserUpdateRequest.Password"/> indicates and OAuth only user.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse{TResult}"/> for the created <see cref="UpdatedUser"/>.</returns>
		RequirementsGated<AuthorityResponse<UpdatedUser>> Create(
			UserCreateRequest createRequest,
			bool? needZeroLengthPasswordWithOAuthConnections,
			CancellationToken cancellationToken);

		/// <summary>
		/// Updates a <see cref="User"/>.
		/// </summary>
		/// <param name="updateRequest">The <see cref="UserUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse{TResult}"/> for the created <see cref="UpdatedUser"/>.</returns>
		RequirementsGated<AuthorityResponse<UpdatedUser>> Update(UserUpdateRequest updateRequest, CancellationToken cancellationToken);
	}
}
