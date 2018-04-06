using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="OSIdentifier"/>
	/// </summary>
	[TestClass]
	public sealed class TestOSIdentifier
	{
		[TestMethod]
		public void TestOSIdentification()
		{
			var identifier = new OSIdentifier();
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), identifier.IsWindows);
		}
	}
}
