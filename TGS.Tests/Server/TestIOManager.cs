using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace TGS.Server.Tests
{
	/// <summary>
	/// Tests for <see cref="IOManager"/>
	/// </summary>
	[TestClass]
	public class TestIOManager
	{
		IOManager IO;
		string tempDir;

		[TestInitialize]
		public void Init()
		{
			tempDir = Path.GetTempFileName();
			File.Delete(tempDir);
			Directory.CreateDirectory(tempDir);
			IO = new IOManager();
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(tempDir, true);
		}

		//This method has a story behind it's existence...
		[TestMethod]
		public void TestSymlinkedDirectoriesWontBeRecursivelyDeleted()
		{
			IO.CreateDirectory(Path.Combine(tempDir, "FakeStatic"));
			IO.CreateDirectory(Path.Combine(tempDir, "FakeGame"));
			IO.WriteAllText(Path.Combine(tempDir, "FakeStatic", "config.txt"), "FakeConfig").Wait();
			IO.CreateSymlink(Path.Combine(tempDir, "FakeGame", "FakeStatic"), Path.Combine(tempDir, "FakeStatic"));
			IO.DeleteDirectory(Path.Combine(tempDir, "FakeGame")).Wait();
			Assert.IsFalse(IO.DirectoryExists(Path.Combine(tempDir, "FakeGame")));
			Assert.IsTrue(IO.FileExists(Path.Combine(tempDir, "FakeStatic", "config.txt")));
		}

		[TestMethod]
		public void TestCreateDirectoryAndDirectoryExists()
		{
			var p = Path.Combine(tempDir, "FakePath");
			Assert.IsFalse(IO.DirectoryExists(p));
			IO.CreateDirectory(p);
			Assert.IsTrue(IO.DirectoryExists(p));
		}

		[TestMethod]
		public void TestFileExistReadAllTextAndWriteAllText()
		{
			var p = Path.Combine(tempDir, "FakePath");
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
			var p = Path.Combine(tempDir, "FakePath");
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
			var p1 = Path.Combine(tempDir, "FakePath1");
			var p2 = Path.Combine(tempDir, "FakePath2");
			var p3 = Path.Combine(tempDir, "dir", "qwre");
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
	}
}
