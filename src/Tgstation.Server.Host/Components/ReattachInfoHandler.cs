using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
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
		/// The <see cref="Api.Models.Instance"/> for the <see cref="ReattachInfoHandler"/>
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// Construct a <see cref="ReattachInfoHandler"/>
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="metadata">The value of <see cref="metadata"/></param>
		public ReattachInfoHandler(IDatabaseContextFactory databaseContextFactory, IDmbFactory dmbFactory, Api.Models.Instance metadata)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		/// <inheritdoc />
		public Task Save(WatchdogReattachInformation reattachInformation, CancellationToken cancellationToken) => databaseContextFactory.UseContext(async (db) =>
		{
			var instance = new Models.Instance { Id = metadata.Id };
			db.Instances.Attach(instance);

			Models.ReattachInformation ConvertReattachInfo(ReattachInformation wdInfo)
			{
				if (wdInfo == null)
					return null;
				db.CompileJobs.Attach(wdInfo.Dmb.CompileJob);
				return new Models.ReattachInformation
				{
					AccessIdentifier = wdInfo.AccessIdentifier,
					ChatChannelsJson = wdInfo.ChatChannelsJson,
					ChatCommandsJson = wdInfo.ChatCommandsJson,
					ServerCommandsJson = wdInfo.ServerCommandsJson,
					CompileJob = wdInfo.Dmb.CompileJob,
					IsPrimary = wdInfo.IsPrimary,
					Port = wdInfo.Port,
					ProcessId = wdInfo.ProcessId,
					RebootState = wdInfo.RebootState
				};
			}

			instance.WatchdogReattachInformation = new Models.WatchdogReattachInformation
			{
				Alpha = ConvertReattachInfo(reattachInformation.Alpha),
				Bravo = ConvertReattachInfo(reattachInformation.Bravo),
				AlphaIsActive = reattachInformation.AlphaIsActive,
			};
			await db.Save(cancellationToken).ConfigureAwait(false);
		});

		/// <inheritdoc />
		public async Task<WatchdogReattachInformation> Load(CancellationToken cancellationToken)
		{
			Models.WatchdogReattachInformation result = null;
			await databaseContextFactory.UseContext(async (db) =>
				result = await db.Instances.Where(x => x.Id == metadata.Id).Select(x => x.WatchdogReattachInformation).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
			).ConfigureAwait(false);

			if (result == default)
				throw new JobException("Unable to load reattach information!");

			var bravoDmbTask = dmbFactory.FromCompileJob(result.Bravo.CompileJob, cancellationToken);
			return new WatchdogReattachInformation(result, await dmbFactory.FromCompileJob(result.Alpha.CompileJob, cancellationToken).ConfigureAwait(false), await bravoDmbTask.ConfigureAwait(false));
		}
	}
}
