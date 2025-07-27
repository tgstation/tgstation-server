using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestFilesystemLinkFactory
	{
		static IFilesystemLinkFactory linkFactory;

		[ClassInitialize]
		public static void SelectFactory(TestContext _)
		{
			var fileSystem = new FileSystem();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				linkFactory = new WindowsFilesystemLinkFactory(fileSystem);
			else
				linkFactory = new PosixFilesystemLinkFactory(fileSystem);
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
		public async Task TestHardLinkWorks()
		{
			string f2 = null;
			var f1 = Path.GetTempFileName();
			try
			{
				f2 = f1 + ".linked";
				const string Text = "Hello world";
				File.WriteAllText(f1, Text);

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					await Assert.ThrowsExceptionAsync<NotSupportedException>(() => linkFactory.CreateHardLink(f1, f2, CancellationToken.None));
					Assert.Inconclusive("Windows does not support hardlinks");
				}

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateHardLink(null, null, CancellationToken.None));
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateHardLink(f1, null, CancellationToken.None));

				await linkFactory.CreateHardLink(f1, f2, default);
				Assert.IsTrue(File.Exists(f2));

				var f2Contents = File.ReadAllText(f2);
				Assert.AreEqual(Text, f2Contents);

				const string NewText = "asdf";
				File.WriteAllText(f1, NewText);

				f2Contents = File.ReadAllText(f2);
				Assert.AreEqual(NewText, f2Contents);

				const string NewText2 = "fdsa";
				File.WriteAllText(f2, NewText2);

				var f1Contents = File.ReadAllText(f1);
				Assert.AreEqual(NewText2, f1Contents);

				File.Delete(f1);
				Assert.IsFalse(File.Exists(f1));
				Assert.IsTrue(File.Exists(f2));
				f2Contents = File.ReadAllText(f2);
				Assert.AreEqual(NewText2, f2Contents);
			}
			finally
			{
				File.Delete(f2);
				File.Delete(f1);
			}
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

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateSymbolicLink(null, null, default));
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateSymbolicLink(f1, null, default));

				await linkFactory.CreateSymbolicLink(f1, f2, default);
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

				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateSymbolicLink(null, null, default));
				await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => linkFactory.CreateSymbolicLink(f1, null, default));

				await linkFactory.CreateSymbolicLink(f1, f2, default);

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
				await linkFactory.CreateSymbolicLink(BadPath, BadPath, default);
				Assert.Fail("No exception thrown!");
			}
			catch { }
		}
	}
}
