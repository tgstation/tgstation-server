using System;

using Microsoft.Extensions.Options;

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
		public GeneralConfiguration GeneralConfiguration => generalConfigurationOptions.Value;

		/// <inheritdoc />
		public DatabaseConfiguration DatabaseConfiguration => databaseConfigurationOptions.Value;

		/// <inheritdoc />
		public SecurityConfiguration SecurityConfiguration => securityConfigurationOptions.Value;

		/// <inheritdoc />
		public SwarmConfiguration SwarmConfiguration => swarmConfigurationOptions.Value;

		/// <inheritdoc />
		public SessionConfiguration SessionConfiguration => sessionConfigurationOptions.Value;

		/// <inheritdoc />
		public FileLoggingConfiguration FileLoggingConfiguration => fileLoggingConfigurationOptions.Value;

		/// <inheritdoc />
		public InternalConfiguration InternalConfiguration => internalConfigurationOptions.Value;

		/// <inheritdoc />
		public ElasticsearchConfiguration ElasticsearchConfiguration => elasticsearchConfigurationOptions.Value;

		/// <inheritdoc />
		public bool ReloadRequired { get; set; }

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="GeneralConfiguration"/>.
		/// </summary>
		readonly IOptions<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="DatabaseConfiguration"/>.
		/// </summary>
		readonly IOptions<DatabaseConfiguration> databaseConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="SecurityConfiguration"/>.
		/// </summary>
		readonly IOptions<SecurityConfiguration> securityConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="FileLoggingConfiguration"/>.
		/// </summary>
		readonly IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="ElasticsearchConfiguration"/>.
		/// </summary>
		readonly IOptions<ElasticsearchConfiguration> elasticsearchConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="InternalConfiguration"/>.
		/// </summary>
		readonly IOptions<InternalConfiguration> internalConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="SwarmConfiguration"/>.
		/// </summary>
		readonly IOptions<SwarmConfiguration> swarmConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="InternalConfiguration"/>.
		/// </summary>
		readonly IOptions<SessionConfiguration> sessionConfigurationOptions;

		/// <summary>
		/// Initializes a new instance of the <see cref="PostSetupServices"/> class.
		/// </summary>
		/// <param name="platformIdentifier">The value of <see cref="PlatformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="GeneralConfiguration"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="DatabaseConfiguration"/>.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="SecurityConfiguration"/>.</param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="FileLoggingConfiguration"/>.</param>
		/// <param name="elasticsearchConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="ElasticsearchConfiguration"/>.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="InternalConfiguration"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="SwarmConfiguration"/>.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="SessionConfiguration"/>.</param>
		public PostSetupServices(
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<DatabaseConfiguration> databaseConfigurationOptions,
			IOptions<SecurityConfiguration> securityConfigurationOptions,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions,
			IOptions<ElasticsearchConfiguration> elasticsearchConfigurationOptions,
			IOptions<InternalConfiguration> internalConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			IOptions<SessionConfiguration> sessionConfigurationOptions)
		{
			PlatformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.databaseConfigurationOptions = databaseConfigurationOptions ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.securityConfigurationOptions = securityConfigurationOptions ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));
			this.fileLoggingConfigurationOptions = fileLoggingConfigurationOptions ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
			this.elasticsearchConfigurationOptions = elasticsearchConfigurationOptions ?? throw new ArgumentNullException(nameof(elasticsearchConfigurationOptions));
			this.internalConfigurationOptions = internalConfigurationOptions ?? throw new ArgumentNullException(nameof(internalConfigurationOptions));
			this.swarmConfigurationOptions = swarmConfigurationOptions ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.sessionConfigurationOptions = sessionConfigurationOptions ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));
		}
	}
}
