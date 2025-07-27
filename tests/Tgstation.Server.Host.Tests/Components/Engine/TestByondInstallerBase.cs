using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Components.Engine.Tests
{
	[TestClass]
	public sealed class TestByondInstallerBase
	{
		[TestMethod]
		public void TestUrlTemplateFormatting()
		{
			const string OSMarker = "TempleOS";

			Assert.AreEqual(
				new Uri("https://example.com/$515.1111_Hello Worl$d.zip"),
				ByondInstallerBase.GetDownloadZipUrl(
					new Version(515, 1111),
					"https://example.com/$$${Major}.${Minor}_${TempleOS:Hello Worl$$d}.zip${Linux:Not this}${Or This}",
					OSMarker));
		}
	}
}
