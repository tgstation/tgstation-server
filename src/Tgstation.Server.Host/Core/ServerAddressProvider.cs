using System;
using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerAddressProvider : IServerAddressProvider
	{
		/// <inheritdoc />
		public Uri HttpBindAddress => generalConfiguration.BindAddress;

		/// <inheritdoc />
		public IPEndPoint AddressEndPoint { get; }

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ServerAddressProvider"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerAddressProvider"/> class.
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
		public ServerAddressProvider(
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IConfiguration configuration)
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
			if (httpEndpoint != null)
				throw new InvalidOperationException("The old Kestrel configuration section is no longer supported, you must remove it. Please set your API port and bind address using the \"General:ApiPort\" \"General:BindAddress\" configuration options!");
			if (generalConfiguration.BindAddress == default)
				throw new InvalidOperationException("Missing required configuration option General:BindAddress!");
			if (!IPEndPoint.TryParse(generalConfiguration.BindAddress.ToString(), out var theIPEndPoint))
				throw new InvalidOperationException("Could not parse the bind address, double check the value of General:BindAddress!");

			AddressEndPoint = theIPEndPoint;
		}
	}
}
