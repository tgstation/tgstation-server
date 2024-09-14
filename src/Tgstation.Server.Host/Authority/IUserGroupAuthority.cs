using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for managing <see cref="UserGroup"/>s.
	/// </summary>
	public interface IUserGroupAuthority : IAuthority
	{
		/// <summary>
		/// Gets the current <see cref="UserGroup"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="UserGroup"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<AuthorityResponse<UserGroup>> Read();

		/// <summary>
		/// Gets the <see cref="UserGroup"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="UserGroup"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		public ValueTask<AuthorityResponse<UserGroup>> GetId(long id, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all registered <see cref="UserGroup"/>s.
		/// </summary>
		/// <returns>A <see cref="IQueryable{T}"/> of <see cref="UserGroup"/>s.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		IQueryable<UserGroup> Queryable();
	}
}
