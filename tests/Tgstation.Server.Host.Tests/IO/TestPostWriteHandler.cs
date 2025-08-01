using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Unix;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestPostWriteHandler
	{
		[TestMethod]
		public void TestThrowsWithNullArg()
		{
			IPostWriteHandler postWriteHandler;
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			if (isWindows)
				postWriteHandler = new WindowsPostWriteHandler();
			else
				postWriteHandler = new PosixPostWriteHandler(Mock.Of<ILogger<PosixPostWriteHandler>>());

			Assert.ThrowsExactly<ArgumentNullException>(() => postWriteHandler.HandleWrite(null));
			Assert.ThrowsExactly<ArgumentNullException>(() => postWriteHandler.NeedsPostWrite(null));
		}

		[TestMethod]
		public void TestPostWrite()
		{
			IPostWriteHandler postWriteHandler;
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			if (isWindows)
				postWriteHandler = new WindowsPostWriteHandler();
			else
				postWriteHandler = new PosixPostWriteHandler(Mock.Of<ILogger<PosixPostWriteHandler>>());

			//test on a valid file first
			var tmpFile = Path.GetTempFileName();
			try
			{
				Assert.IsFalse(postWriteHandler.NeedsPostWrite(tmpFile));
				postWriteHandler.HandleWrite(tmpFile);

				if (isWindows)
					return; //you do nothing

				//ensure it is now executable
				Assert.IsTrue(postWriteHandler.NeedsPostWrite(tmpFile));
				File.WriteAllBytes(tmpFile, Encoding.UTF8.GetBytes("#!/bin/sh\n"));

				using (var process = Process.Start(tmpFile))
				{
					process.WaitForExit();
					Assert.AreEqual(0, process.ExitCode);
				}

				//run it again for the code coverage on that part where no changes are made if it's already executable
				postWriteHandler.HandleWrite(tmpFile);
			}
			finally
			{
				File.Delete(tmpFile);
			}
		}

		[TestMethod]
		public void TestThrowsOnUnix()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				Assert.Inconclusive("POSIX only test.");

			var postWriteHandler = new PosixPostWriteHandler(Mock.Of<ILogger<PosixPostWriteHandler>>());
			var tmpFile = Path.GetTempFileName();
			File.Delete(tmpFile);
			Assert.ThrowsExactly<UnixIOException>(() => postWriteHandler.HandleWrite(tmpFile));
		}
	}
}
