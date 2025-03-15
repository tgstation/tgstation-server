using System;
using System.Net.Http;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class GrpcChannelFactory : IGrpcChannelFactory
	{
		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="GrpcChannelFactory"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="GrpcChannelFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GrpcChannelFactory"/>.
		/// </summary>
		readonly ILogger<GrpcChannelFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="GrpcChannelFactory"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public GrpcChannelFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILogger<GrpcChannelFactory> logger)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public GrpcChannel CreateChannel(Uri address, Func<string> authorization)
		{
			ArgumentNullException.ThrowIfNull(address);
			ArgumentNullException.ThrowIfNull(authorization);

			logger.LogTrace("Creating gRPC channel for {address}...", address);

			var credentials = CallCredentials.FromInterceptor(
				(context, metadata) =>
				{
					var authorizationValue = authorization();
					metadata.Add(HeaderNames.Authorization, authorizationValue);
					return Task.CompletedTask;
				});

			var httpClient = httpClientFactory.CreateClient();
			try
			{
				var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
				{
					Credentials = ChannelCredentials.Create(
						address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
							? ChannelCredentials.SecureSsl
							: ChannelCredentials.Insecure,
						credentials),
					DisposeHttpClient = true,
					HttpClient = httpClient,
					LoggerFactory = loggerFactory,
					ThrowOperationCanceledOnCancellation = true,
					UnsafeUseInsecureChannelCallCredentials = true,
				});

				return channel;
			}
			catch
			{
				httpClient.Dispose();
				throw;
			}
		}
	}
}
