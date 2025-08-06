using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Common.Extensions.Tests
{
	/// <summary>
	/// Tests for <see cref="VersionExtensions"/>.
	/// </summary>
	[TestClass]
	public sealed class TestVersionExtensions
	{
		[TestMethod]
		public void TestSemver()
		{
			Assert.AreEqual(new Version(1, 2, 3), new Version(1, 2, 3, 4).Semver());
		}
	}
}
