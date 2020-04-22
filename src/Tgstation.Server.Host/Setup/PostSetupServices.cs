using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Setup
{
	/// <inheritdoc />
	sealed class PostSetupServices : IPostSetupServices
	{
		/// <inheritdoc />
		public IPlatformIdentifier PlatformIdentifier { get; }

		/// <inheritdoc />
		public GeneralConfiguration GeneralConfiguration { get; }

		/// <inheritdoc />
		public DatabaseConfiguration DatabaseConfiguration { get; }

		/// <inheritdoc />
		public FileLoggingConfiguration FileLoggingConfiguration { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PostSetupServices"/> <see langword="class"/>.
		/// </summary>
		/// <param name="platformIdentifier">The value of <see cref="PlatformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="GeneralConfiguration"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="DatabaseConfiguration"/>.</param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="FileLoggingConfiguration"/>.</param>
		public PostSetupServices(
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<DatabaseConfiguration> databaseConfigurationOptions,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions)
		{
			PlatformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			GeneralConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			DatabaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			FileLoggingConfiguration = fileLoggingConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
		}
	}
}
