using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="IDatabaseCollection{TModel}"/> <see langword="interface"/>.
	/// </summary>
	static class DatabaseCollectionExtensions
	{
		/// <summary>
		/// Gets the unattached, unpopulated <see cref="User"/> with the name <see cref="User.TgsSystemUserName"/>.
		/// </summary>
		/// <param name="databaseCollection">The <see cref="IDatabaseCollection{TModel}"/> of <see cref="User"/>s to operate on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the unattached TGS <see cref="User"/> on success, <see langword="null"/> on failure.</returns>
		public static Task<User> GetTgsUser(this IDatabaseCollection<User> databaseCollection, CancellationToken cancellationToken)
			=> databaseCollection
				.AsQueryable()
				.Where(x => x.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				.Select(x => new User
				{
					Id = x.Id,
				})
				.FirstAsync(cancellationToken);
	}
}
