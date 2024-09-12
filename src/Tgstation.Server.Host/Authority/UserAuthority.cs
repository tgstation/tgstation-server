using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc />
	sealed class UserAuthority : AuthorityBase, IUserAuthority
	{
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="IUsersDataLoader"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IUsersDataLoader dataLoader;

		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// Implements the <see cref="dataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyCollection{T}"/> of <see cref="User"/> <see cref="Api.Models.EntityId.Id"/>s to load.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="User"/>s.</returns>
		[DataLoader]
		public static async ValueTask<Dictionary<long, User>> GetUsers(
			IReadOnlyList<long> ids,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(databaseContext);

			return await databaseContext
				.Users
				.AsQueryable()
				.Where(x => ids.Contains(x.Id!.Value))
				.ToDictionaryAsync(user => user.Id!.Value, cancellationToken);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserAuthority"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="dataLoader">The value of <see cref="dataLoader"/>.</param>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		public UserAuthority(
			ILogger<UserAuthority> logger,
			IDatabaseContext databaseContext,
			IUsersDataLoader dataLoader,
			IAuthenticationContext authenticationContext)
			: base(logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
		}

		/// <inheritdoc />
		public ValueTask<AuthorityResponse<User>> Read(CancellationToken cancellationToken)
			=> ValueTask.FromResult(new AuthorityResponse<User>(authenticationContext.User));

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<User>> GetId(long id, bool includeJoins, bool allowSystemUser, CancellationToken cancellationToken)
		{
			User? user;
			if (includeJoins)
			{
				var queryable = Queryable(true, true);

				user = await queryable.FirstOrDefaultAsync(
					dbModel => dbModel.Id == id,
					cancellationToken);
			}
			else
				user = await dataLoader.LoadAsync(id, cancellationToken);

			if (user == default)
				return NotFound<User>();

			if (!allowSystemUser && user.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				return Forbid<User>();

			return new AuthorityResponse<User>(user);
		}

		/// <inheritdoc />
		public IQueryable<User> Queryable(bool includeJoins)
			=> Queryable(includeJoins, false);

		/// <summary>
		/// Gets all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="allowSystemUser">If the <see cref="User"/> with the <see cref="User.TgsSystemUserName"/> should be included in results.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of <see cref="User"/>s.</returns>
		IQueryable<User> Queryable(bool includeJoins, bool allowSystemUser)
		{
			var tgsUserCanonicalName = User.CanonicalizeName(User.TgsSystemUserName);
			var queryable = databaseContext
				.Users
				.AsQueryable();

			if (!allowSystemUser)
				queryable = queryable
					.Where(user => user.CanonicalName != tgsUserCanonicalName);

			if (includeJoins)
				queryable = queryable
					.Include(x => x.CreatedBy)
					.Include(x => x.OAuthConnections)
					.Include(x => x.Group!)
						.ThenInclude(x => x.PermissionSet)
					.Include(x => x.PermissionSet);

			return queryable;
		}
	}
}
