using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <inheritdoc />
	sealed class CommandFactory : ICommandFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IByondManager byondManager;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="CommandFactory"/>
		/// </summary>
		readonly Models.Instance instance;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="CommandFactory"/>
		/// </summary>
		IWatchdog watchdog;

		/// <summary>
		/// Construct a <see cref="CommandFactory"/>
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		/// <param name="byondManager">The value of <see cref="byondManager"/></param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public CommandFactory(
			IAssemblyInformationProvider assemblyInformationProvider,
			IByondManager byondManager,
			IRepositoryManager repositoryManager,
			IDatabaseContextFactory databaseContextFactory,
			Models.Instance instance)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.byondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <summary>
		/// Set a <paramref name="watchdog"/> for the <see cref="CommandFactory"/>
		/// </summary>
		/// <param name="watchdog">The <see cref="IWatchdog"/> to set</param>
		public void SetWatchdog(IWatchdog watchdog)
		{
			if (this.watchdog != null)
				throw new InvalidOperationException("SetWatchdog has already been called!");
			this.watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
		}

		/// <inheritdoc />
		public IReadOnlyList<ICommand> GenerateCommands()
		{
			if (watchdog == null)
				throw new InvalidOperationException("SetWatchdog has not been called!");
			return new List<ICommand>
			{
				new VersionCommand(assemblyInformationProvider),
				new ByondCommand(byondManager, watchdog),
				new RevisionCommand(watchdog, repositoryManager),
				new PullRequestsCommand(watchdog, repositoryManager, databaseContextFactory, instance),
				new KekCommand()
			};
		}
	}
}