using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.System.Tests
{
	/// <summary>
	/// Tests for <see cref="IProcessFeatures"/>.
	/// </summary>
	[TestClass]
	public sealed class TestProcessFeatures
	{
		IProcessFeatures features;

		[TestInitialize]
		public void Init()
		{
			features = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? (IProcessFeatures)new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
				: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => null), new DefaultIOManager(new AssemblyInformationProvider()), Mock.Of<ILogger<PosixProcessFeatures>>());
		}

		[TestMethod]
		public async Task TestGetUsername()
		{
			if (!new PlatformIdentifier().IsWindows)
				Assert.Inconclusive("This test is buggy on linux and not required");

			var username = await features.GetExecutingUsername(global::System.Diagnostics.Process.GetCurrentProcess(), default);
			Assert.IsTrue(username.Contains(Environment.UserName), $"Exepcted a string containing \"{Environment.UserName}\", got \"{username}\"");
		}
	}
}
