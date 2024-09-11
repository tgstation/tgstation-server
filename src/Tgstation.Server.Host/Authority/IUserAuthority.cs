﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
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
		public ValueTask<AuthorityResponse<User>> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="User"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="User"/>.</param>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="allowSystemUser">If the <see cref="User.TgsSystemUserName"/> may be returned.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		public ValueTask<AuthorityResponse<User>> GetId(long id, bool includeJoins, bool allowSystemUser, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="IQueryable{T}"/> <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		public ValueTask<AuthorityResponse<IQueryable<User>>> List(bool includeJoins);
	}
}
