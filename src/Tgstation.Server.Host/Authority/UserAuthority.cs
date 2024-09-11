using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		/// The <see cref="IAuthenticationContext"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserAuthority"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		public UserAuthority(
			ILogger<UserAuthority> logger,
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext)
			: base(logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
		}

		/// <inheritdoc />
		public ValueTask<AuthorityResponse<User>> Read(CancellationToken cancellationToken)
			=> ValueTask.FromResult(new AuthorityResponse<User>(authenticationContext.User));

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<User>> GetId(long id, bool includeJoins, CancellationToken cancellationToken)
		{
			var queryable = ListCore(includeJoins);

			var user = await queryable.FirstOrDefaultAsync(
				dbModel => dbModel.Id == id,
				cancellationToken);
			if (user == default)
				return NotFound<User>();

			if (user.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				return Forbid<User>();

			return new AuthorityResponse<User>(user);
		}

		/// <inheritdoc />
		public ValueTask<AuthorityResponse<IQueryable<User>>> List(bool includeJoins)
		{
			var systemUserCanonicalName = User.CanonicalizeName(User.TgsSystemUserName);
			return ValueTask.FromResult(
				new AuthorityResponse<IQueryable<User>>(
					ListCore(includeJoins)
						.Where(x => x.CanonicalName != systemUserCanonicalName)));
		}

		/// <summary>
		/// Generates an <see cref="IQueryable{T}"/> for listing <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <returns>A new <see cref="IQueryable{T}"/> of <see cref="User"/>s.</returns>
		private IQueryable<User> ListCore(bool includeJoins)
		{
			var queryable = databaseContext
				.Users
				.AsQueryable();

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
