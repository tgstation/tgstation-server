using Microsoft.AspNetCore.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Console
{
	/// <summary>
	/// Contains the entrypoint for the application
	/// </summary>
	static class Program
	{
		/// <summary>
		/// The <see cref="IServerFactory"/> for the <see cref="Program"/>
		/// </summary>
		internal static IServerFactory ServerFactory { get; set; } = new ServerFactory();

		/// <summary>
		/// Entrypoint for the application
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		internal static async Task Main()
		{
			using (var server = ServerFactory.CreateServer())
				await server.RunAsync().ConfigureAwait(false);
		}
	}
}
