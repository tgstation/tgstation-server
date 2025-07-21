using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.System.Tests
{
	/// <summary>
	/// Tests for <see cref="IProcessFeatures"/>.
	/// </summary>
	[TestClass]
	public sealed class TestProcessFeatures
	{
		static IProcessFeatures features;

		[ClassInitialize]
		public static void Init(TestContext _)
		{
			features = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? (IProcessFeatures)new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
				: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => null), new DefaultIOManager(new MockFileSystem()), Mock.Of<ILogger<PosixProcessFeatures>>());
		}

		[TestMethod]
		public void TestGetUsername()
		{
			if (!new PlatformIdentifier().IsWindows)
				Assert.Inconclusive("This test is buggy on linux and not required");

			var username = features.GetExecutingUsername(global::System.Diagnostics.Process.GetCurrentProcess());
			Assert.IsTrue(username.Contains(Environment.UserName), $"Exepcted a string containing \"{Environment.UserName}\", got \"{username}\"");
		}
	}
}
