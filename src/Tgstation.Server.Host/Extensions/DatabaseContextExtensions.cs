using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
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
		public static async Task<CompileJob> MostRecentCompletedCompileJobOrDefault(
			this IDatabaseContext databaseContext,
			Api.Models.Instance instance,
			CancellationToken cancellationToken)
		{
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));

			var baseQuery = databaseContext
				.CompileJobs
				.Where(x => x.Job.Instance.Id == instance.Id);
			if (databaseContext.DatabaseType == DatabaseType.Sqlite)
			{
				// This is a hack
				// This is a hack and I hate it
				// SQLite can't order by DateTimeOffset so we have to do it ourselves
				// I decided to make this space efficient because "webscale"
				DateTimeOffset? mostRecentCompileJobStoppedAt = null;
				long? mostRecentCompileJobId = null;
				await baseQuery
					.Include(cj => cj.Job)
					.Select(cj => new CompileJob
					{
						Id = cj.Id,
						Job = new Job
						{
							StoppedAt = cj.Job.StoppedAt
						}
					})
					.ForEachAsync(compileJob =>
						{
							if (!mostRecentCompileJobId.HasValue
								|| mostRecentCompileJobStoppedAt.Value < compileJob.Job.StoppedAt.Value)
							{
								mostRecentCompileJobStoppedAt = compileJob.Job.StoppedAt;
								mostRecentCompileJobId = compileJob.Id;
							}
						},
						cancellationToken)
					.ConfigureAwait(false);

				if (!mostRecentCompileJobId.HasValue)
					return default;

				return await databaseContext
					.CompileJobs
					.Where(cj => cj.Id == mostRecentCompileJobId.Value)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);
			}

			return await baseQuery
				.OrderByDescending(x => x.Job.StoppedAt)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
