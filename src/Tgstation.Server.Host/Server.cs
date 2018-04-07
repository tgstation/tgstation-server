using Microsoft.AspNetCore.Hosting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer
	{
		/// <inheritdoc />
		public string UpdatePath => null;

		/// <summary>
		/// The <see cref="IWebHostBuilder"/> for the <see cref="Server"/>
		/// </summary>
		readonly IWebHostBuilder webHostBuilder;

		/// <summary>
		/// Construct a <see cref="Server"/>
		/// </summary>
		/// <param name="webHostBuilder">The value of <see cref="webHostBuilder"/></param>
		public Server(IWebHostBuilder webHostBuilder) => this.webHostBuilder = webHostBuilder ?? throw new ArgumentNullException(nameof(webHostBuilder));

		/// <inheritdoc />
        [ExcludeFromCodeCoverage]
		public async Task RunAsync(CancellationToken cancellationToken)
		{
			using (var webHost = webHostBuilder.UseStartup<Application>().Build())
				await webHost.RunAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
