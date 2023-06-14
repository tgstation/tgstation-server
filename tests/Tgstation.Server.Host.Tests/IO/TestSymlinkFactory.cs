using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestSymlinkFactory
	{
		static ISymlinkFactory symlinkFactory;

		[ClassInitialize]
		public static void SelectFactory(TestContext _)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				symlinkFactory = new WindowsSymlinkFactory();
			else
				symlinkFactory = new PosixSymlinkFactory();
		}

		public static bool HasPermissionToMakeSymlinks()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return true;
			var identity = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		[TestMethod]
		public async Task TestFileWorks()
		{
			if (!HasPermissionToMakeSymlinks())
				Assert.Inconclusive("Current user does not have permission to create symlinks!");
			const string Text = "Hello world";
			string f2 = null;
			var f1 = Path.GetTempFileName();
			try
			{
				f2 = f1 + ".linked";
				File.WriteAllText(f1, Text);

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => symlinkFactory.CreateSymbolicLink(null, null, default));
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => symlinkFactory.CreateSymbolicLink(f1, null, default));

				await symlinkFactory.CreateSymbolicLink(f1, f2, default);
				Assert.IsTrue(File.Exists(f2));

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
		public async Task TestDirectoryWorks()
		{
			if (!HasPermissionToMakeSymlinks())
				Assert.Inconclusive("Current user does not have permission to create symlinks!");
			const string FileName = "TestFile.txt";
			const string Text = "Hello world";
			string f2 = null;
			var f1 = Path.GetTempFileName();
			File.Delete(f1);
			Directory.CreateDirectory(f1);
			try
			{
				f2 = f1 + ".linked";
				var p1 = Path.Combine(f1, FileName);
				File.WriteAllText(p1, Text);

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => symlinkFactory.CreateSymbolicLink(null, null, default));
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => symlinkFactory.CreateSymbolicLink(f1, null, default));

				await symlinkFactory.CreateSymbolicLink(f1, f2, default);

				var p2 = Path.Combine(f2, FileName);
				Assert.IsTrue(File.Exists(p2));

				var f2Contents = File.ReadAllText(p2);
				Assert.AreEqual(Text, f2Contents);

				File.Delete(p2);
				Assert.IsFalse(File.Exists(p1));
			}
			finally
			{
				Directory.Delete(f2, true);
				Directory.Delete(f1, true);
			}
		}

		[TestMethod]
		public async Task TestFailsProperly()
		{
			const string BadPath = "/../../?>!@#$%^&*()O(UF+}P{{??>/////";

			try
			{
				await symlinkFactory.CreateSymbolicLink(BadPath, BadPath, default);
				Assert.Fail("No exception thrown!");
			}
			catch { }
		}
	}
}
