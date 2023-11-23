using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.System;

#nullable disable

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class AbstractHttpClientFactory : IAbstractHttpClientFactory
	{
		/// <summary>
		/// The real <see cref="IHttpClientFactory"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="AbstractHttpClientFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AbstractHttpClientFactory"/>.
		/// </summary>
		readonly ILogger<AbstractHttpClientFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="AbstractHttpClientFactory"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AbstractHttpClientFactory(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<AbstractHttpClientFactory> logger)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
#pragma warning disable IDE0079
#pragma warning disable CA2000
		public IHttpClient CreateClient()
		{
			logger.LogTrace("Creating client...");
			var innerClient = httpClientFactory.CreateClient();
			try
			{
				var client = new Tgstation.Server.Common.Http.HttpClient(innerClient);
				innerClient = null;
				try
				{
					client.DefaultRequestHeaders.UserAgent.Add(assemblyInformationProvider.ProductInfoHeaderValue);
					return client;
				}
				catch
				{
					client.Dispose();
					throw;
				}
			}
			catch
			{
				innerClient?.Dispose();
				throw;
			}
		}
#pragma warning restore CA2000
#pragma warning restore IDE0079
	}
}
