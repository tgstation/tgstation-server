﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying the installed Byond version
	/// </summary>
	sealed class ByondCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "byond";

		/// <inheritdoc />
		public string HelpText => "Displays the running Byond version. Use --active for the version used in future deployments";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="ByondCommand"/>
		/// </summary>
		readonly IByondManager byondManager;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ByondCommand"/>
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// Construct a <see cref="ByondCommand"/>
		/// </summary>
		/// <param name="byondManager">The value of <see cref="byondManager"/></param>
		/// <param name="watchdog">The value of <see cref="watchdog"/></param>
		public ByondCommand(IByondManager byondManager, IWatchdog watchdog)
		{
			this.byondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
		}

		/// <inheritdoc />
		public Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			if (arguments.Split(' ').Any(x => x.ToUpperInvariant() == "--ACTIVE"))
				return Task.FromResult(byondManager.ActiveVersion == null ? "None!" : String.Format(CultureInfo.InvariantCulture, "{0}.{1}", byondManager.ActiveVersion.Major, byondManager.ActiveVersion.Minor));
			if (watchdog.Status == WatchdogStatus.Offline)
				return Task.FromResult("Server offline!");
			return Task.FromResult(watchdog.ActiveCompileJob.ByondVersion);
		}
	}
}
