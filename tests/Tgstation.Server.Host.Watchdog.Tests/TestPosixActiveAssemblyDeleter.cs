using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="PosixActiveAssemblyDeleter"/>
	/// </summary>
	[TestClass]
	public sealed class TestPosixActiveAssemblyDeleter
	{
		[TestMethod]
		public void TestAssemblyDeletion()
		{
			var ourAssembly = GetType().Assembly;
			var fakeAssemblyPath = String.Concat(ourAssembly.Location, Guid.NewGuid());
			File.Copy(ourAssembly.Location, fakeAssemblyPath);

			try
			{
				var deleter = new PosixActiveAssemblyDeleter();
				deleter.DeleteActiveAssembly(fakeAssemblyPath);
				Assert.IsFalse(File.Exists(fakeAssemblyPath));
			}
			catch
			{
				File.Delete(fakeAssemblyPath);
				throw;
			}
		}

		[TestMethod]
		public void TestNullInvoke()
		{
			var deleter = new PosixActiveAssemblyDeleter();
			Assert.ThrowsException<ArgumentNullException>(() => deleter.DeleteActiveAssembly(null));
		}
	}
}
