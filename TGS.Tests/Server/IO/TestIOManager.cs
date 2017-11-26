using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace TGS.Server.IO.Tests
{
	/// <summary>
	/// Tests for <see cref="IOManager"/>
	/// </summary>
	[TestClass]
	public class TestIOManager
	{
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
		public void TestUnlink()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");

			IO.Touch(p1).Wait();
			IO.CreateSymlink(p2, p1).Wait();

			Assert.ThrowsException<AggregateException>(() => IO.Unlink(p3).Wait());
			Assert.ThrowsException<AggregateException>(() => IO.Unlink(p1).Wait());
			IO.Unlink(p2).Wait();

			Assert.IsFalse(IO.FileExists(p2).Result);
			Assert.IsTrue(IO.FileExists(p1).Result);

			IO.CreateDirectory(p2).Wait();
			IO.CreateSymlink(p3, p2).Wait();
			Assert.ThrowsException<AggregateException>(() => IO.Unlink(p1).Wait());
			IO.Unlink(p3).Wait();
			Assert.IsTrue(IO.DirectoryExists(p2).Result);
			Assert.IsFalse(IO.DirectoryExists(p3).Result);
		}

		[TestMethod]
		public void TestUnzip()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(p1, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");
			IO.CreateDirectory(p1).Wait();
			IO.WriteAllText(p2, "asdf").Wait();
			ZipFile.CreateFromDirectory(p1, p3);
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
		public void TestDownloadFile()
		{
			const string URL = "https://raw.githubusercontent.com/Dextraspace/Test/0c81cf5863d98ca9b544086d61c648817af4fb19/README.md";
			const string expected = "# Test\nThis is a test\n\nThis is another test\n\nThis is a third test\n\nOh look another test\n\nasdf\n\nhonk out date again\n\nhi cyber";

			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");

			using (var cts = new CancellationTokenSource())
				IO.DownloadFile(URL, p1, cts.Token).Wait();

			var res = IO.ReadAllText(p1).Result;

			Assert.AreEqual(expected, res);

			IO.DeleteFile(p1).Wait();

			using (var cts = new CancellationTokenSource())
			{
				cts.Cancel();
				//very bigly
				IO.DownloadFile("https://raw.githubusercontent.com/tgstation/tgstation/c1c908fd5810f8e6fe8e78a3c078075b168d3b9a/tgui/assets/tgui.js", p1, cts.Token).Wait();
			}

			if(IO.FileExists(p1).Result)
				using(var F = File.Open(IO.ResolvePath(p1), FileMode.Open))
					Assert.IsTrue(F.Seek(0, SeekOrigin.End) < 1024);

			using (var cts = new CancellationTokenSource())
				Assert.ThrowsException<AggregateException>(() => IO.DownloadFile("http://not.a.url", p1, cts.Token).Wait());
		}

		[TestMethod]
		public void TestMoveDirectory()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(p1, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");
			var p4 = IOManager.ConcatPath(p3, "FakePath2");

			IO.CreateDirectory(p1).Wait();
			IO.WriteAllText(p2, "fasdf").Wait();

			IO.MoveDirectory(p1, p3).Wait();

			Assert.IsFalse(IO.DirectoryExists(p1).Result);
			Assert.AreEqual("fasdf", IO.ReadAllText(p4).Result);

			var p5 = "M:\\FakePath";

			Assert.ThrowsException<AggregateException>(() => IO.MoveDirectory(p3, p5).Wait());
		}

		[TestMethod]
		public void TestCopyFile()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");

			IO.WriteAllText(p2, "fasdf").Wait();

			IO.CopyFile(p2, p1, false, false).Wait();

			Assert.AreEqual("fasdf", IO.ReadAllText(p1).Result);
			Assert.AreEqual("fasdf", IO.ReadAllText(p2).Result);

			IO.WriteAllText(p2, "asdfg").Wait();

			Assert.ThrowsException<AggregateException>(() => IO.CopyFile(p2, p1, false, false).Wait());

			IO.CopyFile(p2, p1, true, false).Wait();

			Assert.AreEqual("asdfg", IO.ReadAllText(p1).Result);
			Assert.AreEqual("asdfg", IO.ReadAllText(p2).Result);

			var p3 = IOManager.ConcatPath(tempDir, "FakePath3", "File");

			Assert.ThrowsException<AggregateException>(() => IO.CopyFile(p2, p3, false, false).Wait());

			IO.CopyFile(p2, p3, false, true).Wait();

			Assert.AreEqual("asdfg", IO.ReadAllText(p3).Result);
			
			IO.WriteAllText(p2, "fasdf").Wait();

			Assert.ThrowsException<AggregateException>(() => IO.CopyFile(p2, p3, false, true).Wait());
			
			IO.CopyFile(p2, p3, true, true).Wait();
			Assert.AreEqual("fasdf", IO.ReadAllText(p3).Result);
		}

		[TestMethod]
		public void TestWriteReadText()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			IO.WriteAllText(p1, "asdfasdf").Wait();
			Assert.AreEqual("asdfasdf", IO.ReadAllText(p1).Result);
		}

		[TestMethod]
		public void TestWriteReadBytes()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var bytes = Encoding.UTF8.GetBytes("asdfasdf");
			IO.WriteAllBytes(p1, bytes).Wait();
			var res = IO.ReadAllBytes(p1).Result;
			Assert.IsTrue(bytes.SequenceEqual(res));
		}
	}
}
