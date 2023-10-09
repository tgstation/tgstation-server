using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying the installed Byond version.
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
		/// The <see cref="IEngineManager"/> for the <see cref="ByondCommand"/>.
		/// </summary>
		readonly IEngineManager engineManager;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ByondCommand"/>.
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondCommand"/> class.
		/// </summary>
		/// <param name="engineManager">The value of <see cref="engineManager"/>.</param>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		public ByondCommand(IEngineManager engineManager, IWatchdog watchdog)
		{
			this.engineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
		}

		/// <inheritdoc />
		public ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			if (arguments.Split(' ').Any(x => x.Equals("--active", StringComparison.OrdinalIgnoreCase)))
			{
				string text;
				if (engineManager.ActiveVersion == null)
					text = "None!";
				else
					switch (engineManager.ActiveVersion.Engine.Value)
					{
						case EngineType.OpenDream:
							text = $"OpenDream: {engineManager.ActiveVersion.SourceCommittish}";
							break;
						case EngineType.Byond:
							text = $"BYOND {engineManager.ActiveVersion.Version.Major}.{engineManager.ActiveVersion.Version.Minor}";
							if (engineManager.ActiveVersion.Version.Build != -1)
								text += " (Custom Build)";

							break;
						default:
							throw new InvalidOperationException($"Invalid EngineType: {engineManager.ActiveVersion.Engine.Value}");
					}

				return ValueTask.FromResult(
					new MessageContent
					{
						Text = text,
					});
			}

			if (watchdog.Status == WatchdogStatus.Offline)
				return ValueTask.FromResult(
					new MessageContent
					{
						Text = "Server offline!",
					});
			return ValueTask.FromResult(
				new MessageContent
				{
					Text = watchdog.ActiveCompileJob?.ByondVersion ?? "None!",
				});
		}
	}
}
