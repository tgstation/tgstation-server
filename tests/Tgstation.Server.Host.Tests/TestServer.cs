using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Tests
{
	[TestClass]
	public sealed class TestServer
	{
		[TestMethod]
		public async Task TestRunAndDispose()
		{
			using (var server = new Server())
				await server.RunAsync().ConfigureAwait(false);
		}
	}
}
