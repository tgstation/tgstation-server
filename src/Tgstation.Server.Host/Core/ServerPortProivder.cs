using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerPortProivder : IServerPortProvider
	{
		/// <inheritdoc />
		public ushort HttpApiPort => generalConfiguration.ApiPort;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ServerPortProivder"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerPortProivder"/> <see langword="class"/>.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		public ServerPortProivder(
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IConfiguration configuration,
			ILogger<ServerPortProivder> logger)
		{
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var httpEndpoint = configuration
				.GetSection("Kestrel")
				.GetSection("EndPoints")
				.GetSection("Http")
				.GetSection("Url")
				.Value;

			if (generalConfiguration.ApiPort == default && httpEndpoint == null)
				throw new InvalidOperationException("Missing required configuration option General:ApiPort!");

			if (generalConfiguration.ApiPort != default)
				return;

			logger.LogWarning("The \"Kestrel\" configuration section is deprecated! Please set your API port using the \"General:ApiPort\" configuration option!");

			var splits = httpEndpoint.Split(":", StringSplitOptions.RemoveEmptyEntries);
			var portString = splits.Last();
			portString = portString.TrimEnd('/');

			if (!UInt16.TryParse(portString, out var result))
				throw new InvalidOperationException($"Failed to parse HTTP EndPoint port: {httpEndpoint}");

			generalConfiguration.ApiPort = result;
		}
	}
}
