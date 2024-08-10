using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying <see cref="Api.Models.Internal.RevisionInformation"/>.
	/// </summary>
	sealed class RevisionCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "revision";

		/// <inheritdoc />
		public string HelpText => "Display live origin commit sha. Add --repo to view repository revision";

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
		/// Initializes a new instance of the <see cref="RevisionCommand"/> class.
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		public RevisionCommand(IWatchdog watchdog, IRepositoryManager repositoryManager)
		{
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
		}

		/// <inheritdoc />
		public async ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			string result;
			if (arguments.Split(' ').Any(x => x.Equals("--repo", StringComparison.OrdinalIgnoreCase)))
			{
				if (repositoryManager.CloneInProgress || repositoryManager.InUse)
					return new MessageContent
					{
						Text = "Repository busy! Try again later",
					};

				using var repo = await repositoryManager.LoadRepository(cancellationToken);
				if (repo == null)
					return new MessageContent
					{
						Text = "Repository unavailable!",
					};
				result = repo.Head;
			}
			else
			{
				if (watchdog.Status == WatchdogStatus.Offline)
					return new MessageContent
					{
						Text = "Server offline!",
					};
				result = watchdog.ActiveCompileJob?.RevisionInformation.OriginCommitSha!;
			}

			return new MessageContent
			{
				Text = String.Format(CultureInfo.InvariantCulture, "^{0}", result),
			};
		}
	}
}
