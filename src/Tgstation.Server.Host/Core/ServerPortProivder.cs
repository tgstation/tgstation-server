using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerPortProivder : IServerPortProvider
	{
		/// <inheritdoc />
		public ushort HttpApiPort { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerPortProivder"/> <see langword="class"/>.
		/// </summary>
		/// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		public ServerPortProivder(IConfiguration configuration, ILogger<ServerPortProivder> logger)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			// Log the active configuration.
			logger.LogInformation("Active Config:");

			void LogSection(IConfigurationSection section, string prefix)
			{
				prefix = $"{prefix}{section.Key}:";
				if (section.Value != null)
					logger.LogInformation("{0} {1}", prefix, section.Value);
				else
				{
					foreach (var child in section.GetChildren())
						LogSection(child, prefix);
				}
			}

			foreach (var section in configuration.GetChildren())
				LogSection(section, String.Empty);

			var httpEndpoint = configuration
				.GetSection("Kestrel")
				.GetSection("EndPoints")
				.GetSection("Http")
				.GetSection("Url")
				.Value;

			if (httpEndpoint == null)
				throw new InvalidOperationException("Missing required configuration option Kestrel:EndPoints:Http:Url!");

			var splits = httpEndpoint.Split(":", StringSplitOptions.RemoveEmptyEntries);
			var portString = splits.Last();
			portString = portString.TrimEnd('/');

			if (!UInt16.TryParse(portString, out var result))
				throw new InvalidOperationException($"Failed to parse HTTP EndPoint port: {httpEndpoint}");

			HttpApiPort = result;
		}
	}
}
