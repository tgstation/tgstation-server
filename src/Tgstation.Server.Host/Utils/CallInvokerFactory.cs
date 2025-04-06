using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class CallInvokerFactory : ICallInvokerlFactory, IDisposable
	{
		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="CallInvokerFactory"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="CallInvokerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="CallInvokerFactory"/>.
		/// </summary>
		readonly ILogger<CallInvokerFactory> logger;

		/// <summary>
		/// The cache of active <see cref="GrpcChannel"/>s.
		/// </summary>
		readonly ConcurrentDictionary<Uri, GrpcChannel> channels;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallInvokerFactory"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public CallInvokerFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILogger<CallInvokerFactory> logger)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			channels = new ConcurrentDictionary<Uri, GrpcChannel>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var kvp in channels)
				kvp.Value.Dispose();
		}

		/// <inheritdoc />
		public CallInvoker CreateCallInvoker(Uri address, Func<string> authorization)
		{
			ArgumentNullException.ThrowIfNull(address);
			ArgumentNullException.ThrowIfNull(authorization);

			var channel = channels.GetOrAdd(
				address,
				_ =>
				{
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
				});

			return channel.CreateCallInvoker();
		}
	}
}
