using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Command for reading the active <see cref="TestMerge"/>s.
	/// </summary>
	sealed class PullRequestsCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "prs";

		/// <inheritdoc />
		public string HelpText => "Display live test merge numbers. Add --repo to view test merges in the repository as opposed to live. Add --staged to view PRs in the upcoming deployment.";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="PullRequestsCommand"/>.
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="PullRequestsCommand"/>.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="PullRequestsCommand"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="ILatestCompileJobProvider"/> for the <see cref="PullRequestsCommand"/>.
		/// </summary>
		readonly ILatestCompileJobProvider compileJobProvider;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="PullRequestsCommand"/>.
		/// </summary>
		readonly Models.Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="PullRequestsCommand"/> class.
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="compileJobProvider">The value of <see cref="compileJobProvider"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public PullRequestsCommand(
			IWatchdog watchdog,
			IRepositoryManager repositoryManager,
			IDatabaseContextFactory databaseContextFactory,
			ILatestCompileJobProvider compileJobProvider,
			Models.Instance instance)
		{
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.compileJobProvider = compileJobProvider ?? throw new ArgumentNullException(nameof(compileJobProvider));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		// TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			IEnumerable<(int Number, string TargetCommitSha)> results;
			var splits = arguments.Split(' ');
			var hasRepo = splits.Any(x => x.Equals("--repo", StringComparison.OrdinalIgnoreCase));
			var hasStaged = splits.Any(x => x.Equals("--staged", StringComparison.OrdinalIgnoreCase));

			if (hasStaged && hasRepo)
				return new MessageContent
				{
					Text = "Please use only one of `--staged` or `--repo`",
				};

			if (hasRepo)
			{
				if (repositoryManager.CloneInProgress || repositoryManager.InUse)
					return new MessageContent
					{
						Text = "Repository busy! Try again later",
					};

				string head;
				using (var repo = await repositoryManager.LoadRepository(cancellationToken))
				{
					if (repo == null)
						return new MessageContent
						{
							Text = "Repository unavailable!",
						};

					head = repo.Head;
				}

				results = null!;
				await databaseContextFactory.UseContext(
					async db =>
					{
						var anonResults = await db
							.RevisionInformations
							.Where(x => x.Instance!.Id == instance.Id && x.CommitSha == head)
							.SelectMany(x => x.ActiveTestMerges!)
							.Select(x => x.TestMerge)
							.Select(x => new
							{
								x.Number,
								TargetCommitSha = x.TargetCommitSha!,
							})
							.ToListAsync(cancellationToken);
						results = anonResults
							.Select(anonResult => (anonResult.Number, anonResult.TargetCommitSha));
					});
			}
			else if (watchdog.Status == WatchdogStatus.Offline)
				return new MessageContent
				{
					Text = "Server offline!",
				};
			else
			{
				var compileJobToUse = watchdog.ActiveCompileJob;
				if (hasStaged)
				{
					var latestCompileJob = await compileJobProvider.LatestCompileJob();
					if (latestCompileJob?.Id != compileJobToUse?.Id)
						compileJobToUse = latestCompileJob;
					else
						compileJobToUse = null;
				}

				results = compileJobToUse
					?.RevisionInformation
					.ActiveTestMerges
					?.Select(x => (x.TestMerge.Number, TargetCommitSha: x.TestMerge.TargetCommitSha!))
					.ToList()
					?? Enumerable.Empty<(int Number, string TargetCommitSha)>();
			}

			return new MessageContent
			{
				Text = !results.Any()
					? "None!"
					: String.Join(
						", ",
						results.Select(
							x => $"#{x.Number} at {x.TargetCommitSha![..7]}")),
			};
		}
#pragma warning restore CA1506
	}
}
