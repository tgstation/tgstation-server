using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Console.Debug
{
    static class Program
    {
		internal static IServerFactory serverFactory = new ServerFactory();

        static async Task Main(string[] args)
        {
			using (var server = serverFactory.CreateServer(args, "Updates"))
				await server.RunAsync(default).ConfigureAwait(false);
        }
    }
}
