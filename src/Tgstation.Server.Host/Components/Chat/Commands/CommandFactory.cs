using System;
using System.Collections.Generic;
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
		/// Construct a <see cref="CommandFactory"/>
		/// </summary>
		/// <param name="application">The value of <see cref="application"/></param>
		public CommandFactory(IApplication application)
		{
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <inheritdoc />
		public IReadOnlyList<ICommand> GenerateCommands() => new List<ICommand>
		{
			new KekCommand(),
			new VersionCommand(application)
		};
	}
}