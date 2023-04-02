using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;
using System.Threading.Tasks;

using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestIOManager
	{
		readonly IIOManager ioManager = new DefaultIOManager(new AssemblyInformationProvider());

		[TestMethod]
		public async Task TestDeleteDirectory()
		{
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			Directory.CreateDirectory(tempPath);
			try
			{
				await ioManager.DeleteDirectory(tempPath, default);

				Assert.IsFalse(Directory.Exists(tempPath));
			}
			catch
			{
				Directory.Delete(tempPath);
				throw;
			}
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
	}
}
