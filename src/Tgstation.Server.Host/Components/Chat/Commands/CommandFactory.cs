using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <inheritdoc />
	sealed class CommandFactory : ICommandFactory
	{
		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IByondManager byondManager;

		/// <summary>
		/// Construct a <see cref="CommandFactory"/>
		/// </summary>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="byondManager">The value of <see cref="byondManager"/></param>
		public CommandFactory(IApplication application, IByondManager byondManager)
		{
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.byondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
		}

		/// <inheritdoc />
		public IReadOnlyList<ICommand> GenerateCommands() => new List<ICommand>
		{
			new VersionCommand(application),
			new ByondCommand(byondManager),
			new KekCommand()
		};
	}
}