using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestIOManager
	{
		readonly IIOManager ioManager = new DefaultIOManager();

		[TestMethod]
		public async Task TestDeleteDirectory()
		{
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			Directory.CreateDirectory(tempPath);
			try
			{
				await File.WriteAllTextAsync(Path.Combine(tempPath, "file.txt"), "asdf");
				var subDir = Path.Combine(tempPath, "subdir");
				Directory.CreateDirectory(subDir);
				await File.WriteAllTextAsync(Path.Combine(subDir, "file2.txt"), "fdsa");
				await ioManager.DeleteDirectory(tempPath, default);

				Assert.IsFalse(Directory.Exists(tempPath));
			}
			catch
			{
				Directory.Delete(tempPath, true);
				throw;
			}
		}

		[TestMethod]
		public async Task TestDeleteDirectoryWithSymlinkInsideDoesntRecurse()
		{
			var linkFactory = (IFilesystemLinkFactory)(new PlatformIdentifier().IsWindows
				? new WindowsFilesystemLinkFactory()
				: new PosixFilesystemLinkFactory());

			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			Directory.CreateDirectory(tempPath);
			try
			{
				var targetDir = ioManager.ConcatPath(tempPath, "targetdir");
				await ioManager.CreateDirectory(targetDir, CancellationToken.None);
				var fileInTargetDir = ioManager.ConcatPath(targetDir, "test1.txt");

				var expectedBytes = Encoding.UTF8.GetBytes("I want to live");
				await ioManager.WriteAllBytes(fileInTargetDir, expectedBytes, CancellationToken.None);

				var testDir = ioManager.ConcatPath(tempPath, "testdir");
				await ioManager.CreateDirectory(testDir, CancellationToken.None);
				var symlinkedFile = ioManager.ConcatPath(testDir, "test1.txt");
				var symlinkedDir = ioManager.ConcatPath(testDir, "linkedDir");

				await linkFactory.CreateSymbolicLink(targetDir, symlinkedDir, CancellationToken.None);
				await linkFactory.CreateSymbolicLink(fileInTargetDir, symlinkedFile, CancellationToken.None);

				Assert.IsTrue(await ioManager.DirectoryExists(symlinkedDir, CancellationToken.None));
				Assert.IsTrue(await ioManager.FileExists(symlinkedFile, CancellationToken.None));
				Assert.IsTrue(await ioManager.FileExists(ioManager.ConcatPath(symlinkedDir, "test1.txt"), CancellationToken.None));
				Assert.IsTrue(await ioManager.FileExists(fileInTargetDir, CancellationToken.None));

				await ioManager.DeleteDirectory(testDir, CancellationToken.None);

				Assert.IsFalse(await ioManager.DirectoryExists(symlinkedDir, CancellationToken.None));
				Assert.IsFalse(await ioManager.FileExists(symlinkedFile, CancellationToken.None));
				Assert.IsFalse(await ioManager.FileExists(ioManager.ConcatPath(symlinkedDir, "test1.txt"), CancellationToken.None));
				Assert.IsTrue(await ioManager.FileExists(fileInTargetDir, CancellationToken.None));
				Assert.IsTrue(expectedBytes.SequenceEqual((await ioManager.ReadAllBytes(fileInTargetDir, CancellationToken.None)).ToArray()));
			}
			catch
			{
				Directory.Delete(tempPath, true);
				throw;
			}
		}

		[TestMethod]
		public async Task TestFileExists()
		{
			var tempPath = Path.GetTempFileName();
			try
			{
				Assert.IsTrue(await ioManager.FileExists(tempPath, default));
			}
			finally
			{
				File.Delete(tempPath);
			}

			Assert.IsFalse(await ioManager.FileExists(tempPath, default));
		}

		[TestMethod]
		public async Task TestDirectoryExists()
		{
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);

			Assert.IsFalse(await ioManager.DirectoryExists(tempPath, default));

			Directory.CreateDirectory(tempPath);

			try
			{
				Assert.IsTrue(await ioManager.DirectoryExists(tempPath, default));
			}
			catch
			{
				Directory.Delete(tempPath);
				throw;
			}
		}

		[TestMethod]
		public async Task TestCopyDirectoryThrows()
		{
			int? throttle = null;
			var tempPath1 = Guid.NewGuid().ToString();
			var tempPath2 = Guid.NewGuid().ToString();

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => ioManager.CopyDirectory(
				null,
				null,
				null,
				tempPath2,
				throttle,
				default).AsTask());
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => ioManager.CopyDirectory(
				null,
				null,
				tempPath1,
				null,
				throttle,
				default).AsTask());
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => ioManager.CopyDirectory(
				null,
				null,
				null,
				null,
				throttle,
				default).AsTask());
			await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => ioManager.CopyDirectory(
				null,
				null,
				tempPath1,
				tempPath2,
				-1,
				default).AsTask());
		}

		[TestMethod]
		public async Task TestCopyDirectoryOneTask()
		{
			await TestCopyDirectory(1);
		}

		[TestMethod]
		public async Task TestCopyDirectoryMaxTasks()
		{
			await TestCopyDirectory(Int32.MaxValue);
		}

		[TestMethod]
		public async Task TestCopyDirectoryUnlimitedTasks()
		{
			await TestCopyDirectory(null);
		}

		async Task TestCopyDirectory(int? throttle)
		{
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			Directory.CreateDirectory(tempPath);
			try
			{
				var tempPath2 = Path.GetTempFileName();
				File.Delete(tempPath2);

				await File.WriteAllTextAsync(Path.Combine(tempPath, "file.txt"), "asdf");
				var subDir = Path.Combine(tempPath, "subdir");
				Directory.CreateDirectory(subDir);
				await File.WriteAllTextAsync(Path.Combine(subDir, "file2.txt"), "fdsa");

				try
				{
					await ioManager.CopyDirectory(
						null,
						null,
						tempPath,
						tempPath2,
						throttle,
						default);

					Assert.IsTrue(Directory.Exists(tempPath2));
					var newFilePath = Path.Combine(tempPath2, "file.txt");
					Assert.IsTrue(File.Exists(newFilePath));
					var newFileText = await File.ReadAllTextAsync(newFilePath);
					Assert.AreEqual("asdf", newFileText);
					var newDirPath = Path.Combine(tempPath2, "subdir");
					Assert.IsTrue(Directory.Exists(newDirPath));
					var newFile2Path = Path.Combine(newDirPath, "file2.txt");
					Assert.IsTrue(File.Exists(newFile2Path));
					var newFile2Text = await File.ReadAllTextAsync(newFile2Path);
					Assert.AreEqual("fdsa", newFile2Text);
				}
				finally
				{
					Directory.Delete(tempPath2, true);
				}
			}
			finally
			{
				Directory.Delete(tempPath, true);
			}
		}
	}
}
