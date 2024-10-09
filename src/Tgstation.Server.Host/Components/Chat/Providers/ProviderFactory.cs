using System;
using System.Globalization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

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
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="FileLoggingConfiguration"/> for the <see cref="ProviderFactory"/>.
		/// </summary>
		readonly FileLoggingConfiguration loggingConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderFactory"/> class.
		/// </summary>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="loggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="loggingConfiguration"/>.</param>
		public ProviderFactory(
			IJobManager jobManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			ILoggerFactory loggerFactory,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<FileLoggingConfiguration> loggingConfigurationOptions)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			loggingConfiguration = loggingConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(loggingConfigurationOptions));
		}

		/// <inheritdoc />
		public IProvider CreateProvider(Models.ChatBot settings)
		{
			ArgumentNullException.ThrowIfNull(settings);
			return settings.Provider switch
			{
				ChatProvider.Irc => new IrcProvider(
					jobManager,
					asyncDelayer,
					loggerFactory.CreateLogger<IrcProvider>(),
					assemblyInformationProvider,
					settings,
					loggingConfiguration),
				ChatProvider.Discord => new DiscordProvider(
					jobManager,
					asyncDelayer,
					loggerFactory.CreateLogger<DiscordProvider>(),
					assemblyInformationProvider,
					settings,
					generalConfiguration),
				_ => throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider)),
			};
		}
	}
}
