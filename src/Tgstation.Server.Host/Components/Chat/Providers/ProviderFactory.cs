using System;
using System.Globalization;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <inheritdoc />
	sealed class ProviderFactory : IProviderFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderFactory"/> class.
		/// </summary>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		public ProviderFactory(
			IJobManager jobManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			ILoggerFactory loggerFactory)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <inheritdoc />
		public IProvider CreateProvider(Models.ChatBot settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			return settings.Provider switch
			{
				ChatProvider.Irc => new IrcProvider(
					jobManager,
					assemblyInformationProvider,
					asyncDelayer,
					loggerFactory.CreateLogger<IrcProvider>(),
					settings),
				ChatProvider.Discord => new DiscordProvider(
					jobManager,
					assemblyInformationProvider,
					asyncDelayer,
					loggerFactory.CreateLogger<DiscordProvider>(),
					settings),
				_ => throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider)),
			};
		}
	}
}
