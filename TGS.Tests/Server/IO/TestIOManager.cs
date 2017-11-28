using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Server.IO.Tests
{
	/// <summary>
	/// Tests for <see cref="IOManager"/>
	/// </summary>
	[TestClass]
	public class TestIOManager
	{
		//need a special override sometimes
		sealed class UnresolvingIOManager : IOManager
		{
			public override string ResolvePath(string path)
			{
				return path;
			}
		}

		protected IOManager IO;
		protected string tempDir;

		[TestInitialize]
		public virtual void Init()
		{
			tempDir = Path.GetTempFileName();
			File.Delete(tempDir);
			Directory.CreateDirectory(tempDir);
			IO = new IOManager();
		}

		[TestCleanup]
		public virtual void Cleanup()
		{
			Directory.Delete(tempDir, true);
		}

		//This method has a story behind it's existence...
		[TestMethod]
		public void TestSymlinkedDirectoriesWontBeRecursivelyDeleted()
		{
			IO.CreateDirectory(IOManager.ConcatPath(tempDir, "FakeStatic")).Wait();
			IO.CreateDirectory(IOManager.ConcatPath(tempDir, "FakeGame")).Wait();
			IO.WriteAllText(IOManager.ConcatPath(tempDir, "FakeStatic", "config.txt"), "FakeConfig").Wait();
			IO.CreateSymlink(IOManager.ConcatPath(tempDir, "FakeGame", "FakeStatic"), IOManager.ConcatPath(tempDir, "FakeStatic")).Wait();
			IO.DeleteDirectory(IOManager.ConcatPath(tempDir, "FakeGame")).Wait();
			Assert.IsFalse(IO.DirectoryExists(IOManager.ConcatPath(tempDir, "FakeGame")).Result);
			Assert.IsTrue(IO.FileExists(IOManager.ConcatPath(tempDir, "FakeStatic", "config.txt")).Result);
		}

		[TestMethod]
		public void TestCreateDirectoryAndDirectoryExists()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			Assert.IsFalse(IO.DirectoryExists(p).Result);
			IO.CreateDirectory(p).Wait();
			Assert.IsTrue(IO.DirectoryExists(p).Result);
		}

		[TestMethod]
		public void TestFileExistReadAllTextAndWriteAllText()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			Assert.IsFalse(IO.FileExists(p).Result);
			Assert.ThrowsException<FileNotFoundException>(() => {
				try
				{
					IO.ReadAllText(p).Wait();
				}
				catch (AggregateException e)
				{
					throw e.InnerException;
				}
			});
			IO.WriteAllText(p, "test").Wait();
			Assert.IsTrue(IO.FileExists(p).Result);
			Assert.AreEqual("test", IO.ReadAllText(p).Result);
		}

		[TestMethod]
		public void TestReadAllBytesAndWriteAllBytes()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			var bytes = new byte[] { 1, 2, 3, 4 };
			Assert.IsFalse(IO.FileExists(p).Result);
			Assert.ThrowsException<FileNotFoundException>(() => {
				try
				{
					IO.ReadAllBytes(p).Wait();
				}
				catch (AggregateException e)
				{
					throw e.InnerException;
				}
			});
			IO.WriteAllBytes(p, bytes).Wait();
			Assert.IsTrue(IO.FileExists(p).Result);
			Assert.IsTrue(bytes.SequenceEqual(IO.ReadAllBytes(p).Result));
		}

		[TestMethod]
		public void TestMoveFile()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "dir", "qwre");
			IO.WriteAllText(p1, "asdf").Wait();
			IO.MoveFile(p1, p2, false, false).Wait();
			
			Assert.IsFalse(IO.FileExists(p1).Result);
			Assert.AreEqual("asdf", IO.ReadAllText(p2).Result);

			IO.WriteAllText(p1, "fdsa").Wait();
			IO.MoveFile(p2, p1, true, false).Wait();

			Assert.AreEqual("asdf", IO.ReadAllText(p1).Result);

			IO.MoveFile(p1, p3, false, true).Wait();
			Assert.IsTrue(IO.DirectoryExists(Path.GetDirectoryName(p3)).Result);
			Assert.AreEqual("asdf", IO.ReadAllText(p3).Result);
		}

		[TestMethod]
		public void TestTouch()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			IO.Touch(p1).Wait();
			Assert.IsTrue(IO.FileExists(p1).Result);
		}

		[TestMethod]
		public async Task TestUnlink()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");

			await IO.Touch(p1);
			await IO.CreateSymlink(p2, p1);

			await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => IO.Unlink(p3));
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => IO.Unlink(p1));
			await IO.Unlink(p2);

			Assert.IsFalse(await IO.FileExists(p2));
			Assert.IsTrue(await IO.FileExists(p1));

			await IO.CreateDirectory(p2);
			await IO.CreateSymlink(p3, p2);
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => IO.Unlink(p1));
			await IO.Unlink(p3);
			Assert.IsTrue(await IO.DirectoryExists(p2));
			Assert.IsFalse(await IO.DirectoryExists(p3));
		}

		[TestMethod]
		public void TestUnzip()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(p1, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");
			IO.CreateDirectory(p1).Wait();
			IO.WriteAllText(p2, "asdf").Wait();
			ZipFile.CreateFromDirectory(IO.ResolvePath(p1), IO.ResolvePath(p3));
			IO.DeleteDirectory(p1).Wait();

			Assert.IsTrue(IO.FileExists(p3).Result);

			IO.UnzipFile(p3, p1).Wait();

			Assert.AreEqual("asdf", IO.ReadAllText(p2).Result);
		}

		[TestMethod]
		public void TestGetUrl()
		{
			const string URL = "https://raw.githubusercontent.com/Dextraspace/Test/0c81cf5863d98ca9b544086d61c648817af4fb19/README.md";
			const string expected = "# Test\nThis is a test\n\nThis is another test\n\nThis is a third test\n\nOh look another test\n\nasdf\n\nhonk out date again\n\nhi cyber";

			var res = IO.GetURL(URL).Result;

			Assert.AreEqual(expected, res);
		}

		[TestMethod]
		public async Task TestDownloadFile()
		{
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");
			IO.WriteAllText(p2, "asdf").Wait();

			var URL = "file:///" + IO.ResolvePath(p2).Replace('\\', '/');

			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			
			using (var cts = new CancellationTokenSource())
				IO.DownloadFile(URL, p1, cts.Token).Wait();

			var res = await IO.ReadAllText(p1);

			Assert.AreEqual("asdf", res);

			await IO.DeleteFile(p1);

			using (var cts = new CancellationTokenSource())
			{
				cts.Cancel();
				//very bigly
				await IO.DownloadFile("https://raw.githubusercontent.com/tgstation/tgstation/c1c908fd5810f8e6fe8e78a3c078075b168d3b9a/tgui/assets/tgui.js", p1, cts.Token);
			}

			if (await IO.FileExists(p1))
				using (var F = File.Open(IO.ResolvePath(p1), FileMode.Open))
					Assert.IsTrue(F.Seek(0, SeekOrigin.End) < 1024 * 500);

			using (var cts = new CancellationTokenSource())
				await Assert.ThrowsExceptionAsync<WebException>(() => IO.DownloadFile("http://not.a.url", p1, cts.Token));
		}

		[TestMethod]
		public async Task TestMoveDirectory()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(p1, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");
			var p4 = IOManager.ConcatPath(p3, "FakePath2");

			await IO.CreateDirectory(p1);
			await IO.WriteAllText(p2, "fasdf");

			await IO.MoveDirectory(p1, p3);

			Assert.IsFalse(await IO.DirectoryExists(p1));
			Assert.AreEqual("fasdf", await IO.ReadAllText(p4));

			var p5 = "M:\\FakePath";

			await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => IO.MoveDirectory(p3, p5));
		}

		[TestMethod]
		public async Task TestCopyFile()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");

			await IO.WriteAllText(p2, "fasdf");

			IO.CopyFile(p2, p1, false, false).Wait();

			Assert.AreEqual("fasdf", await IO.ReadAllText(p1));
			Assert.AreEqual("fasdf", await IO.ReadAllText(p2));

			await IO.WriteAllText(p2, "asdfg");

			await Assert.ThrowsExceptionAsync<IOException>(() => IO.CopyFile(p2, p1, false, false));

			await IO.CopyFile(p2, p1, true, false);

			Assert.AreEqual("asdfg", await IO.ReadAllText(p1));
			Assert.AreEqual("asdfg", await IO.ReadAllText(p2));

			var p3 = IOManager.ConcatPath(tempDir, "FakePath3", "File");

			await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => IO.CopyFile(p2, p3, false, false));

			await IO.CopyFile(p2, p3, false, true);

			Assert.AreEqual("asdfg", await IO.ReadAllText(p3));

			await IO.WriteAllText(p2, "fasdf");

			await Assert.ThrowsExceptionAsync<IOException>(() => IO.CopyFile(p2, p3, false, true));

			await IO.CopyFile(p2, p3, true, true);
			Assert.AreEqual("fasdf", await IO.ReadAllText(p3));
		}

		[TestMethod]
		public void TestWriteReadAllText()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			IO.WriteAllText(p1, "asdfasdf").Wait();
			Assert.AreEqual("asdfasdf", IO.ReadAllText(p1).Result);
		}

		[TestMethod]
		public void TestWriteReadAllBytes()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var bytes = Encoding.UTF8.GetBytes("asdfasdf");
			IO.WriteAllBytes(p1, bytes).Wait();
			var res = IO.ReadAllBytes(p1).Result;
			Assert.IsTrue(bytes.SequenceEqual(res));
		}

		[TestMethod]
		public void TestAppendAllText()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			IO.WriteAllText(p1, "asdfasdf").Wait();
			IO.AppendAllText(p1, "fdsafdsa").Wait();
			Assert.AreEqual("asdfasdffdsafdsa", IO.ReadAllText(p1).Result);
		}

		[TestMethod]
		public async Task TestCreateSymlink()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");

			var tmpIO = IO;

			try
			{
				IO = new UnresolvingIOManager();
				//Intentional weird escape to make the thing error
				await Assert.ThrowsExceptionAsync<SymlinkException>(() => IO.CreateSymlink("L\\\\asdf", "M\\\\asdf"));
			}
			finally
			{
				IO = tmpIO;
			}
			await IO.WriteAllText(p1, "asdf");

			await IO.CreateSymlink(p2, p1);

			Assert.AreEqual("asdf", await IO.ReadAllText(p2));

			await IO.DeleteFile(p1);
			await IO.DeleteFile(p2);

			var tmpTempDir = IOManager.ConcatPath(tempDir, "Folder");
			p1 = IOManager.ConcatPath(tmpTempDir, "FakePath1");

			await IO.CreateDirectory(tmpTempDir);

			await IO.WriteAllText(p1, "asdf");
			tmpTempDir = IOManager.ConcatPath(tempDir, "Folder2");

			await IO.CreateSymlink(tmpTempDir, IOManager.ConcatPath(tempDir, "Folder"));

			p2 = IOManager.ConcatPath(tmpTempDir, "FakePath1");

			Assert.AreEqual(await IO.ReadAllText(p1), await IO.ReadAllText(p2));
		}

		[TestMethod]
		public void TestGetDirectoryName()
		{
			var dir = IOManager.ConcatPath("asdf", "fdsa");
			Assert.AreEqual("asdf", IOManager.GetDirectoryName(dir));
		}

		[TestMethod]
		public void TestDeleteFile()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath2");

			IO.Touch(p1).Wait();
			IO.DeleteFile(p1).Wait();
			Assert.IsFalse(IO.FileExists(p1).Result);

			IO.DeleteFile(p2).Wait();
		}

		[TestMethod]
		public async Task TestDeleteDirectory()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath2");
			var p4 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath4");

			await IO.CreateDirectory(p1);
			await IO.DeleteDirectory(p1);
			Assert.IsFalse(await IO.DirectoryExists(p1));

			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => IO.DeleteDirectory(p1, false, new List<string> { null }));

			await IO.CreateDirectory(p1);
			await IO.CreateSymlink(p3, p1);

			//double is intentional
			await IO.DeleteDirectory(p3);
			await IO.DeleteDirectory(p3);

			await IO.Touch(p2);
			await IO.Touch(p4);

			await IO.DeleteDirectory(p1, true, new List<string> { "FakePath4" });
			Assert.IsFalse(await IO.FileExists(p2));
			Assert.IsTrue(await IO.FileExists(p4));

			await IO.DeleteFile(p4);
			await IO.CreateDirectory(p2);
			await IO.CreateDirectory(p4);

			await IO.DeleteDirectory(p1, true, new List<string> { "FakePath2" });
		}

		[TestMethod]
		public async Task TestCopyDirectory()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath3");
			var p4 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath4");
			var p5 = IOManager.ConcatPath(tempDir, "FakePath5");
			var p6 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath6");
			var p7 = IOManager.ConcatPath(tempDir, "FakePath1", "FakePath7");

			await IO.CreateDirectory(p1);
			await IO.WriteAllText(p2, "asdf");
			await IO.Touch(p3);
			await IO.CreateDirectory(p4);
			await IO.CreateDirectory(p6);
			await IO.Touch(p7);

			await IO.CopyDirectory(p1, p5);
			Assert.IsTrue(await IO.DirectoryExists(p5));
			Assert.AreEqual("asdf", await IO.ReadAllText(IOManager.ConcatPath(p5, "FakePath2")));

			await IO.DeleteDirectory(p5);

			await IO.CopyDirectory(p1, p5, new List<string> { "FakePath3", "FakePath6" });

			Assert.IsTrue(await IO.DirectoryExists(IOManager.ConcatPath(p5, "FakePath4")));
			Assert.IsFalse(await IO.FileExists(IOManager.ConcatPath(p5, "FakePath3")));
			Assert.IsTrue(await IO.FileExists(IOManager.ConcatPath(p5, "FakePath7")));
			Assert.IsFalse(await IO.DirectoryExists(IOManager.ConcatPath(p5, "FakePath6")));

			await IO.DeleteDirectory(p1);
			await IO.DeleteDirectory(p5);

			await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => IO.CopyDirectory(p1, p5));
			await IO.CopyDirectory(p1, p5, null, true);
		}
	}
}
