using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Console.Tests
{
	[TestClass]
	public class TestProgram
	{
		[TestMethod]
		public async Task TestProgramRuns()
		{
			await Program.Main().ConfigureAwait(false);
		}
	}
}
