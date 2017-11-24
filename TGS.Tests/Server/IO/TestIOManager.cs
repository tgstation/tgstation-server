using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
			IO.CreateDirectory(IOManager.ConcatPath(tempDir, "FakeStatic"));
			IO.CreateDirectory(IOManager.ConcatPath(tempDir, "FakeGame"));
			IO.WriteAllText(IOManager.ConcatPath(tempDir, "FakeStatic", "config.txt"), "FakeConfig").Wait();
			IO.CreateSymlink(IOManager.ConcatPath(tempDir, "FakeGame", "FakeStatic"), IOManager.ConcatPath(tempDir, "FakeStatic"));
			IO.DeleteDirectory(IOManager.ConcatPath(tempDir, "FakeGame")).Wait();
			Assert.IsFalse(IO.DirectoryExists(IOManager.ConcatPath(tempDir, "FakeGame")));
			Assert.IsTrue(IO.FileExists(IOManager.ConcatPath(tempDir, "FakeStatic", "config.txt")));
		}

		[TestMethod]
		public void TestCreateDirectoryAndDirectoryExists()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			Assert.IsFalse(IO.DirectoryExists(p));
			IO.CreateDirectory(p);
			Assert.IsTrue(IO.DirectoryExists(p));
		}

		[TestMethod]
		public void TestFileExistReadAllTextAndWriteAllText()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			Assert.IsFalse(IO.FileExists(p));
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
			Assert.IsTrue(IO.FileExists(p));
			Assert.AreEqual("test", IO.ReadAllText(p).Result);
		}

		[TestMethod]
		public void TestReadAllBytesAndWriteAllBytes()
		{
			var p = IOManager.ConcatPath(tempDir, "FakePath");
			var bytes = new byte[] { 1, 2, 3, 4 };
			Assert.IsFalse(IO.FileExists(p));
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
			Assert.IsTrue(IO.FileExists(p));
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
			
			Assert.IsFalse(IO.FileExists(p1));
			Assert.AreEqual("asdf", IO.ReadAllText(p2).Result);

			IO.WriteAllText(p1, "fdsa").Wait();
			IO.MoveFile(p2, p1, true, false).Wait();

			Assert.AreEqual("asdf", IO.ReadAllText(p1).Result);

			IO.MoveFile(p1, p3, false, true).Wait();
			Assert.IsTrue(IO.DirectoryExists(Path.GetDirectoryName(p3)));
			Assert.AreEqual("asdf", IO.ReadAllText(p3).Result);
		}

		[TestMethod]
		public void TestTouch()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			IO.Touch(p1).Wait();
			Assert.IsTrue(IO.FileExists(p1));
		}

		[TestMethod]
		public void TestUnlink()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(tempDir, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");

			IO.Touch(p1).Wait();
			IO.CreateSymlink(p2, p1);

			Assert.ThrowsException<FileNotFoundException>(() => IO.Unlink(p3));
			Assert.ThrowsException<InvalidOperationException>(() => IO.Unlink(p1));
			IO.Unlink(p2);

			Assert.IsFalse(IO.FileExists(p2));
			Assert.IsTrue(IO.FileExists(p1));

			IO.CreateDirectory(p2);
			IO.CreateSymlink(p3, p2);
			Assert.ThrowsException<InvalidOperationException>(() => IO.Unlink(p1));
			IO.Unlink(p3);
			Assert.IsTrue(IO.DirectoryExists(p2));
			Assert.IsFalse(IO.DirectoryExists(p3));
		}

		[TestMethod]
		public void TestUnzip()
		{
			var p1 = IOManager.ConcatPath(tempDir, "FakePath1");
			var p2 = IOManager.ConcatPath(p1, "FakePath2");
			var p3 = IOManager.ConcatPath(tempDir, "FakePath3");
			IO.CreateDirectory(p1);
			IO.WriteAllText(p2, "asdf").Wait();
			ZipFile.CreateFromDirectory(p1, p3);
			IO.DeleteDirectory(p1).Wait();

			Assert.IsTrue(IO.FileExists(p3));

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
	}
}
