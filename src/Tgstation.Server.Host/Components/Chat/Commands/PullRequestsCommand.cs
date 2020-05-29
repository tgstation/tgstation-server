using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Command for reading the active <see cref="Api.Models.TestMerge"/>s
	/// </summary>
	sealed class PullRequestsCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "prs";

		/// <inheritdoc />
		public string HelpText => "Display live test merge pull request numbers. Add --repo to view repository test merges";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="PullRequestsCommand"/>
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="PullRequestsCommand"/>
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="PullRequestsCommand"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="PullRequestsCommand"/>
		/// </summary>
		readonly Models.Instance instance;

		/// <summary>
		/// Construct a <see cref="PullRequestsCommand"/>
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public PullRequestsCommand(IWatchdog watchdog, IRepositoryManager repositoryManager, IDatabaseContextFactory databaseContextFactory, Models.Instance instance)
		{
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		// TODO: Decomplexify
		#pragma warning disable CA1506
		public async Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			IEnumerable<Models.TestMerge> results = null;
			if (arguments.Split(' ').Any(x => x.ToUpperInvariant() == "--REPO"))
			{
				string head;
				using (var repo = await repositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
				{
					if (repo == null)
						return "Repository unavailable!";
					head = repo.Head;
				}

				await databaseContextFactory.UseContext(
					async db => results = await db
						.RevisionInformations
						.AsQueryable()
						.Where(x => x.Instance.Id == instance.Id && x.CommitSha == head)
						.SelectMany(x => x.ActiveTestMerges)
						.Select(x => x.TestMerge)
						.Select(x => new Models.TestMerge
						{
							Number = x.Number,
							PullRequestRevision = x.PullRequestRevision
						})
						.ToListAsync(cancellationToken)
						.ConfigureAwait(false))
					.ConfigureAwait(false);
			}
			else
			{
				if (!watchdog.Running)
					return "Server offline!";
				results = watchdog.ActiveCompileJob?.RevisionInformation.ActiveTestMerges.Select(x => x.TestMerge).ToList() ?? new List<Models.TestMerge>();
			}

			return !results.Any() ? "None!" : String.Join(", ", results.Select(x => String.Format(CultureInfo.InvariantCulture, "#{0} at {1}", x.Number, x.PullRequestRevision.Substring(0, 7))));
		}
		#pragma warning restore CA1506
	}
}
