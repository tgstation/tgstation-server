using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace TGS.Server.Tests
{
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
	}
}
