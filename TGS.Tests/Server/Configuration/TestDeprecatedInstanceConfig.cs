using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using TGS.Server.IO;

namespace TGS.Server.Configuration.Tests
{
	/// <summary>
	/// Tests for <see cref="DeprecatedInstanceConfig"/>
	/// </summary>
	[TestClass]
	public sealed class TestDeprecatedInstanceConfig
	{
		[TestMethod]
		public void TestLoading()
		{
			var mockIO = new Mock<IIOManager>();
			mockIO.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns(() => Task.FromResult("{}"));
			var dic = InstanceConfig.Load(".", mockIO.Object);
			Assert.IsTrue(dic is DeprecatedInstanceConfig);
		}

		[TestMethod]
		public void TestMigration()
		{
			var dic = new DeprecatedInstanceConfig(".");
			var po = new PrivateObject(dic);
			po.SetProperty(nameof(dic.Version), (ulong)0);
			dic.MigrateToCurrentVersion();
			var pt = new PrivateType(typeof(InstanceConfig));
			Assert.AreEqual(pt.GetStaticField("CurrentVersion"), dic.Version);
		}
	}
}
