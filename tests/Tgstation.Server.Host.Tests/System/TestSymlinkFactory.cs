using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.System.Tests
{
	[TestClass]
	public sealed class TestSymlinkFactory
	{
		readonly IFilesystemLinkFactory factory = new PlatformIdentifier().IsWindows
			? new WindowsFilesystemLinkFactory(
				new FileSystem())
			: new PosixFilesystemLinkFactory(
				new FileSystem());

		[TestMethod]
		public async Task TestSymlinks()
		{
			var cancellationToken = CancellationToken.None;
			var cwd = Path.GetTempFileName();
			File.Delete(cwd);
			Directory.CreateDirectory(cwd);
			try
			{
				var realDir = Path.Combine(cwd, "RealDir");
				var symDir = Path.Combine(cwd, "SymDir");
				var realFile = Path.Combine(cwd, "RealFile.txt");
				var symFile = Path.Combine(realDir, "RealFile.txt");

				var subRealFile = Path.Combine(realDir, "test.txt");
				var subSymFile = Path.Combine(symDir, "test.txt");

				try
				{
					await factory.CreateSymbolicLink(subRealFile, subSymFile, cancellationToken);
					Assert.Fail("Expected Exception!");
				}
				catch
				{
				}

				Directory.CreateDirectory(realDir);
				Directory.CreateDirectory(symDir);

				await File.WriteAllBytesAsync(realFile, Array.Empty<byte>(), cancellationToken);
				await File.WriteAllBytesAsync(symFile, Array.Empty<byte>(), cancellationToken);

				try
				{
					await factory.CreateSymbolicLink(realFile, symFile, cancellationToken);
					Assert.Fail("Expected Exception!");
				}
				catch
				{
				}

				Directory.Delete(symDir);
				File.Delete(symFile);

				await factory.CreateSymbolicLink(realFile, symFile, cancellationToken);
				Assert.IsTrue(File.Exists(symFile));
				Assert.IsFalse(Directory.Exists(symFile));

				await File.WriteAllTextAsync(realFile, "test", cancellationToken);
				var symFileContents = await File.ReadAllTextAsync(symFile, cancellationToken);
				Assert.AreEqual("test", symFileContents);
				File.Delete(symFile);
				File.Delete(realFile);

				try
				{
					await factory.CreateSymbolicLink(realDir, symDir, cancellationToken);

					Assert.IsFalse(File.Exists(symDir));
					Assert.IsTrue(Directory.Exists(symDir));

					await File.WriteAllTextAsync(subRealFile, "test", cancellationToken);
					Assert.IsTrue(File.Exists(subSymFile));

					File.Delete(subSymFile);
					Assert.IsFalse(File.Exists(subRealFile));
				}
				finally
				{
					if (factory.SymlinkedDirectoriesAreDeletedAsFiles)
						File.Delete(symDir);
					else
						Directory.Delete(symDir);
				}

				if (factory.SymlinkedDirectoriesAreDeletedAsFiles)
					Assert.IsFalse(File.Exists(symDir));
				else
					Assert.IsFalse(Directory.Exists(symDir));
			}
			finally
			{
				Directory.Delete(cwd, true);
			}
		}
	}
}
