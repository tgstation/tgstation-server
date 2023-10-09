using System;
using System.Collections.Generic;

using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Deployment;
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
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IEngineManager"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly IEngineManager engineManager;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="ILatestCompileJobProvider"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly ILatestCompileJobProvider compileJobProvider;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		readonly Models.Instance instance;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		IWatchdog watchdog;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandFactory"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="engineManager">The value of <see cref="engineManager"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="compileJobProvider">The value of <see cref="compileJobProvider"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public CommandFactory(
			IAssemblyInformationProvider assemblyInformationProvider,
			IEngineManager engineManager,
			IRepositoryManager repositoryManager,
			IDatabaseContextFactory databaseContextFactory,
			ILatestCompileJobProvider compileJobProvider,
			Models.Instance instance)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.engineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.compileJobProvider = compileJobProvider ?? throw new ArgumentNullException(nameof(compileJobProvider));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <summary>
		/// Set a <paramref name="watchdog"/> for the <see cref="CommandFactory"/>.
		/// </summary>
		/// <param name="watchdog">The <see cref="IWatchdog"/> to set.</param>
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
				new ByondCommand(engineManager, watchdog),
				new RevisionCommand(watchdog, repositoryManager),
				new PullRequestsCommand(watchdog, repositoryManager, databaseContextFactory, compileJobProvider, instance),
				new KekCommand(),
			};
		}
	}
}
