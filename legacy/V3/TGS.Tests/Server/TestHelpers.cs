using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using TGServiceTests;

namespace TGS.Server.Tests
{
	/// <summary>
	/// Tests for <see cref="Helpers"/>
	/// </summary>
	[TestClass]
	public sealed class TestHelpers : TempDirectoryRequiredTest
	{
		[TestMethod]
		public void TestGetFilesWithExtensionInDirectory()
		{
			Assert.ThrowsException<ArgumentNullException>(() => Helpers.GetFilesWithExtensionInDirectory(null, null).ToList());
			Assert.ThrowsException<ArgumentNullException>(() => Helpers.GetFilesWithExtensionInDirectory("", null).ToList());
			Assert.ThrowsException<ArgumentNullException>(() => Helpers.GetFilesWithExtensionInDirectory(null, "").ToList());

			Assert.AreEqual(0, Helpers.GetFilesWithExtensionInDirectory("Z:/", "dm").Count());

			Assert.AreEqual(0, Helpers.GetFilesWithExtensionInDirectory(TempPath, "dm").Count());

			File.WriteAllText(Path.Combine(TempPath, "somefile.txt"), "asdf");
			
			Assert.AreEqual(0, Helpers.GetFilesWithExtensionInDirectory(TempPath, "dm").Count());

			File.WriteAllText(Path.Combine(TempPath, "somefile.dm"), "asdf");

			Assert.AreEqual(1, Helpers.GetFilesWithExtensionInDirectory(TempPath, "dm").Count());
		}
	}
}
