using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="IDatabaseContext"/> <see langword="class"/>.
	/// </summary>
	static class DatabaseContextExtensions
	{
		/// <summary>
		/// Get the most recent <see cref="CompileJob"/> for a given <paramref name="instance"/> from a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/>.</param>
		/// <param name="instance">The <see cref="Instance"/> to search for <see cref="CompileJob"/>s.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the most recent <see cref="CompileJob"/> associated with the given <paramref name="instance"/> from the <paramref name="databaseContext"/>.</returns>
		public static Task<CompileJob> MostRecentCompletedCompileJobOrDefault(
			this IDatabaseContext databaseContext,
			Api.Models.Instance instance,
			CancellationToken cancellationToken)
		{
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));

			return databaseContext
				.CompileJobs
				.AsQueryable()
				.Where(x => x.Job.Instance.Id == instance.Id)
				.OrderByDescending(x => x.Job.StoppedAt)
				.FirstOrDefaultAsync(cancellationToken);
		}
	}
}
