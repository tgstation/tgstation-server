using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Api.Models.Internal.Tests
{
	[TestClass]
	public sealed class TestEngineVersion
	{
		[TestMethod]
		public void TestParsing()
		{
			Assert.IsTrue(EngineVersion.TryParse("OpenDream-6894ba0702c1764d333eb52aa0cc211d62e2cb1c-1", out var version));
			Assert.IsNotNull(version);
			Assert.AreEqual(EngineType.OpenDream, version.Engine);
			Assert.AreEqual("6894ba0702c1764d333eb52aa0cc211d62e2cb1c", version.SourceSHA);
			Assert.IsNull(version.Version);
			Assert.AreEqual(1, version.CustomIteration);

			Assert.IsTrue(EngineVersion.TryParse("OpenDream-6894ba0702c1764d333eb52aa0cc211d62e2cb1c", out version));
			Assert.IsNotNull(version);
			Assert.AreEqual(EngineType.OpenDream, version.Engine);
			Assert.AreEqual("6894ba0702c1764d333eb52aa0cc211d62e2cb1c", version.SourceSHA);
			Assert.IsNull(version.Version);
			Assert.IsFalse(version.CustomIteration.HasValue);

			Assert.IsTrue(EngineVersion.TryParse("515.1616", out version));
			Assert.IsNotNull(version);
			Assert.AreEqual(EngineType.Byond, version.Engine);
			Assert.AreEqual(new Version(515, 1616), version.Version);
			Assert.IsNull(version.SourceSHA);
			Assert.IsFalse(version.CustomIteration.HasValue);

			Assert.IsTrue(EngineVersion.TryParse("515.1616.12", out version));
			Assert.IsNotNull(version);
			Assert.AreEqual(EngineType.Byond, version.Engine);
			Assert.AreEqual(new Version(515, 1616), version.Version);
			Assert.IsNull(version.SourceSHA);
			Assert.AreEqual(12, version.CustomIteration);

			Assert.IsFalse(EngineVersion.TryParse("x", out version));
			Assert.IsNull(version);
			Assert.ThrowsExactly<InvalidOperationException>(() => EngineVersion.Parse("x"));
		}
	}
}
