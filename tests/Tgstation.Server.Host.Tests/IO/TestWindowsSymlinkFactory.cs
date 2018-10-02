using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestWindowsSymlinkFactory
	{
		[TestMethod]
		public async Task TestWorks()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				Assert.Inconclusive("Windows only test");

			var factory = new WindowsSymlinkFactory();

			const string Text = "Hello world";
			string f2 = null;
			var f1 = Path.GetTempFileName();
			try
			{
				f2 = f1 + ".linked";
				File.WriteAllText(f1, Text);

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateSymbolicLink(null, null, default)).ConfigureAwait(false);
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateSymbolicLink(f1, null, default)).ConfigureAwait(false);

				await factory.CreateSymbolicLink(f1, f2, default).ConfigureAwait(false);

				var f2Contents = File.ReadAllText(f2);
				Assert.AreEqual(Text, f2Contents);
			}
			finally
			{
				File.Delete(f2);
				File.Delete(f1);
			}
		}

		[TestMethod]
		public async Task TestFailsProperly()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				Assert.Inconclusive("Windows only test");

			var factory = new WindowsSymlinkFactory();

			const string BadPath = "C:/?><:{ }";

			await Assert.ThrowsExceptionAsync<Win32Exception>(() => factory.CreateSymbolicLink(BadPath, BadPath, default)).ConfigureAwait(false);
		}
	}
}
