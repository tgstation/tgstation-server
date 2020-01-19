using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.System.Tests
{
	[TestClass]
	public sealed class TestPlatformIdentifier
	{
		[TestMethod]
		public void TestCorrectPlatform()
		{
			var identifier = new PlatformIdentifier();

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			const string WindowsScriptExtension = "bat";
			const string PosixScriptExtension = "sh";


			Assert.AreEqual(isWindows, identifier.IsWindows);

			Assert.AreEqual(isWindows ? WindowsScriptExtension : PosixScriptExtension, identifier.ScriptFileExtension);
		}
	}
}
