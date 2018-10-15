using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestPlatformIdentifier
	{
		[TestMethod]
		public void TestCorrectPlatform()
		{
			var identifier = new PlatformIdentifier();
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), identifier.IsWindows);
		}
	}
}
