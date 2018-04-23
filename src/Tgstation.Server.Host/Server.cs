using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer, IServerControl
	{
		/// <inheritdoc />
		public string UpdatePath { get; private set; }

		/// <summary>
		/// The <see cref="IWebHostBuilder"/> for the <see cref="Server"/>
		/// </summary>
		readonly IWebHostBuilder webHostBuilder;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="Server"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Construct a <see cref="Server"/>
		/// </summary>
		/// <param name="webHostBuilder">The value of <see cref="webHostBuilder"/></param>
		public Server(IWebHostBuilder webHostBuilder) => this.webHostBuilder = webHostBuilder ?? throw new ArgumentNullException(nameof(webHostBuilder));

		/// <inheritdoc />
        [ExcludeFromCodeCoverage]
		public async Task RunAsync(CancellationToken cancellationToken)
		{
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			using (cancellationTokenSource)
			using (var webHost = webHostBuilder
				.UseStartup<Application>()
				.ConfigureServices((serviceCollection) => serviceCollection.AddSingleton<IServerControl>(this))
				.Build()
			)
				await webHost.RunAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public void ApplyUpdate(string updatePath)
		{
			lock (this)
			{
				if (updatePath != null)
					throw new InvalidOperationException("ApplyUpdate has already been called!");
				UpdatePath = updatePath;
			}
			cancellationTokenSource.Cancel();
		}
	}
}
