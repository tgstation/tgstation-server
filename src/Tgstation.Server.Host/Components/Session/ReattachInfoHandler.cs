using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Database;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class ReattachInfoHandler : IReattachInfoHandler
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="ReattachInfoHandler"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="ReattachInfoHandler"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ReattachInfoHandler"/>
		/// </summary>
		readonly ILogger<ReattachInfoHandler> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="ReattachInfoHandler"/>
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// Construct a <see cref="ReattachInfoHandler"/>
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="metadata">The value of <see cref="metadata"/></param>
		public ReattachInfoHandler(IDatabaseContextFactory databaseContextFactory, IDmbFactory dmbFactory, ILogger<ReattachInfoHandler> logger, Api.Models.Instance metadata)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		/// <inheritdoc />
		public Task Save(DualReattachInformation reattachInformation, CancellationToken cancellationToken) => databaseContextFactory.UseContext(async (db) =>
		{
			if (reattachInformation == null)
				throw new ArgumentNullException(nameof(reattachInformation));

			logger.LogDebug("Saving reattach information: {0}...", reattachInformation);

			await db
				.WatchdogReattachInformations
				.AsQueryable()
				.Where(x => x.InstanceId == metadata.Id)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(false);

			Models.ReattachInformation ConvertReattachInfo(ReattachInformation wdInfo)
			{
				if (wdInfo == null)
					return null;
				db.CompileJobs.Attach(wdInfo.Dmb.CompileJob);
				return new Models.ReattachInformation
				{
					AccessIdentifier = wdInfo.AccessIdentifier,
					CompileJob = wdInfo.Dmb.CompileJob,
					IsPrimary = wdInfo.IsPrimary,
					Port = wdInfo.Port,
					ProcessId = wdInfo.ProcessId,
					RebootState = wdInfo.RebootState,
					LaunchSecurityLevel = wdInfo.LaunchSecurityLevel
				};
			}

			db.WatchdogReattachInformations.Add(new Models.DualReattachInformation
			{
				Alpha = ConvertReattachInfo(reattachInformation.Alpha),
				Bravo = ConvertReattachInfo(reattachInformation.Bravo),
				AlphaIsActive = reattachInformation.AlphaIsActive,
				InstanceId = metadata.Id
			});
			await db.Save(cancellationToken).ConfigureAwait(false);
		});

		/// <inheritdoc />
		public async Task<DualReattachInformation> Load(CancellationToken cancellationToken)
		{
			Models.DualReattachInformation result = null;
			TimeSpan? topicTimeout = null;
			await databaseContextFactory.UseContext(async (db) =>
			{
				IQueryable<Models.Instance> InstanceQuery() => db.Instances
					.AsQueryable()
					.Where(x => x.Id == metadata.Id);

				var timeoutMilliseconds = await InstanceQuery()
					.Select(x => x.DreamDaemonSettings.TopicRequestTimeout)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);

				if (timeoutMilliseconds == default)
					return;

				topicTimeout = TimeSpan.FromMilliseconds(timeoutMilliseconds.Value);

				var instance = await InstanceQuery()
					.Include(x => x.WatchdogReattachInformation).ThenInclude(x => x.Alpha).ThenInclude(x => x.CompileJob)
					.Include(x => x.WatchdogReattachInformation).ThenInclude(x => x.Bravo).ThenInclude(x => x.CompileJob)
					.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				result = instance.WatchdogReattachInformation;
				if (result == default)
					return;
				instance.WatchdogReattachInformation = null;
				db.WatchdogReattachInformations.Remove(result);
				await db.Save(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);

			if (result == default)
			{
				logger.LogDebug("Reattach information not found!");
				return null;
			}

			Task<IDmbProvider> GetDmbForReattachInfo(Models.ReattachInformation reattachInformation) => reattachInformation != null
				? dmbFactory.FromCompileJob(reattachInformation.CompileJob, cancellationToken)
				: Task.FromResult<IDmbProvider>(null);

			var bravoDmbTask = GetDmbForReattachInfo(result.Bravo);
			var info = new DualReattachInformation(
				result,
				await GetDmbForReattachInfo(result.Alpha).ConfigureAwait(false),
				await bravoDmbTask.ConfigureAwait(false))
			{
				TopicRequestTimeout = topicTimeout.Value
			};

			logger.LogDebug("Reattach information loaded: {0}", info);
			return info;
		}
	}
}
