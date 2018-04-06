using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.IO;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="WindowsActiveAssemblyDeleter"/>
	/// </summary>
	[TestClass]
	public sealed class TestWindowsActiveAssemblyDeleter
	{
		[TestMethod]
		public void TestAssemblyDeletion()
		{
			var ourAssembly = GetType().Assembly;
			var fakeAssemblyPath = String.Concat(ourAssembly.Location, Guid.NewGuid());
			File.Copy(ourAssembly.Location, fakeAssemblyPath);

			try
			{
				var deleter = new WindowsActiveAssemblyDeleter();
				try
				{
					deleter.DeleteActiveAssembly(fakeAssemblyPath);
				}
				catch (Win32Exception e)
				{
					Assert.AreEqual(e.NativeErrorCode, 5);
				}
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
			var deleter = new WindowsActiveAssemblyDeleter();
			Assert.ThrowsException<ArgumentNullException>(() => deleter.DeleteActiveAssembly(null));
		}
	}
}
