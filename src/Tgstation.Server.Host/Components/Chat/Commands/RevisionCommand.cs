using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying <see cref="Api.Models.Internal.RevisionInformation"/>
	/// </summary>
	sealed class RevisionCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "revision";

		/// <inheritdoc />
		public string HelpText => "Display live commit sha. Add --repo to view repository revision";

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
		/// Construct a <see cref="RevisionCommand"/>
		/// </summary>
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/></param>
		public RevisionCommand(IWatchdog watchdog, IRepositoryManager repositoryManager)
		{
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
		}

		/// <inheritdoc />
		public async Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			string result;
			if (arguments.Split(' ').Any(x => x.ToUpperInvariant() == "--REPO"))
				using (var repo = await repositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
				{
					if (repo == null)
						return "Repository unavailable!";
					result = repo.Head;
				}
			else
			{
				if (watchdog.Status == WatchdogStatus.Offline)
					return "Server offline!";
				result = watchdog.ActiveCompileJob?.RevisionInformation.CommitSha;
			}

			return String.Format(CultureInfo.InvariantCulture, "^{0}", result);
		}
	}
}
