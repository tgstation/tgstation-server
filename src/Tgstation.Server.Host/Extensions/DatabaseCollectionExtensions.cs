using System;
using System.Linq;
using System.Linq.Expressions;
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
		/// Gets <see cref="User"/> with the name <see cref="User.TgsSystemUserName"/>.
		/// </summary>
		/// <typeparam name="TResult">The transformed return <see cref="Type"/>.</typeparam>
		/// <param name="databaseCollection">The <see cref="IDatabaseCollection{TModel}"/> of <see cref="User"/>s to operate on.</param>
		/// <param name="selector">A selecting <see cref="Expression"/> for transforming the returned <see cref="User"/> into the <typeparamref name="TResult"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the unattached TGS <see cref="User"/> on success, <see langword="null"/> on failure.</returns>
		public static Task<TResult> GetTgsUser<TResult>(
			this IDatabaseCollection<User> databaseCollection,
			Expression<Func<User, TResult>> selector,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(databaseCollection);
			ArgumentNullException.ThrowIfNull(selector);

			return databaseCollection
				.Where(x => x.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				.Select(selector)
				.FirstAsync(cancellationToken);
		}
	}
}
