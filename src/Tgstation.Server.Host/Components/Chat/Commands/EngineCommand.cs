using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying the installed Byond version.
	/// </summary>
	sealed class EngineCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "engine";

		/// <inheritdoc />
		public string HelpText => "Displays the running engine version. Use --active for the version used in future deployments";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IEngineManager"/> for the <see cref="EngineCommand"/>.
		/// </summary>
		readonly IEngineManager engineManager;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="EngineCommand"/>.
		/// </summary>
		readonly IWatchdog watchdog;

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineCommand"/> class.
		/// </summary>
		/// <param name="engineManager">The value of <see cref="engineManager"/>.</param>
		/// <param name="watchdog">The value of <see cref="watchdog"/>.</param>
		public EngineCommand(IEngineManager engineManager, IWatchdog watchdog)
		{
			this.engineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
		}

		/// <inheritdoc />
		public ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			EngineVersion engineVersion;
			if (arguments.Split(' ').Any(x => x.Equals("--active", StringComparison.OrdinalIgnoreCase)))
				engineVersion = engineManager.ActiveVersion;
			else
			{
				if (watchdog.Status == WatchdogStatus.Offline)
					return ValueTask.FromResult(
						new MessageContent
						{
							Text = "Server offline!",
						});

				if (watchdog.ActiveCompileJob == null)
					return ValueTask.FromResult(
						new MessageContent
						{
							Text = "None!",
						});

				if (!EngineVersion.TryParse(watchdog.ActiveCompileJob.EngineVersion, out engineVersion))
					throw new InvalidOperationException($"Invalid engine version: {watchdog.ActiveCompileJob.EngineVersion}");
			}

			string text;
			if (engineVersion == null)
				text = "None!";
			else
				text = engineVersion.Engine.Value switch
				{
					EngineType.OpenDream => $"OpenDream: {engineVersion.SourceSHA}",
					EngineType.Byond => $"BYOND {engineVersion.Version.Major}.{engineVersion.Version.Minor}",
					_ => throw new InvalidOperationException($"Invalid EngineType: {engineVersion.Engine.Value}"),
				};

			if (engineVersion.CustomIteration.HasValue)
				text += $" (Custom Upload #{engineVersion.CustomIteration.Value})";

			return ValueTask.FromResult(
				new MessageContent
				{
					Text = text,
				});
		}
	}
}
