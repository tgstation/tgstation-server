using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using TGS.Server.Configuration;

namespace TGS.Server.IO.Tests
{
	/// <summary>
	/// Tests for <see cref="InstanceIOManager"/>
	/// </summary>
	[TestClass]
	public sealed class TestInstanceIOManager : TestIOManager
	{
		string realTempDir;

		[TestInitialize]
		public override void Init()
		{
			base.Init();
			realTempDir = tempDir;
			var mockInstanceConfig = new Mock<IInstanceConfig>();
			mockInstanceConfig.Setup(x => x.Directory).Returns(realTempDir);
			IO = new InstanceIOManager(mockInstanceConfig.Object);
		}
		
		[TestCleanup]
		public override void Cleanup()
		{
			Directory.Delete(realTempDir, true);
		}

		[TestMethod]
		public void TestResolvePath()
		{
			var di1 = new DirectoryInfo(realTempDir);
			var di2 = new DirectoryInfo(IO.ResolvePath("."));
			Assert.AreEqual(di1.FullName.ToUpperInvariant(), di2.FullName);
		}
	}
}
