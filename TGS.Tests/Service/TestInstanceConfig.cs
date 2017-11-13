using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TGServiceTests;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="InstanceConfig"/>
	/// </summary>
	[TestClass]
	public class TestInstanceConfig : TempDirectoryRequiredTest
	{
		/// <summary>
		/// The path to the <see cref="InstanceConfig"/> JSON at <see cref="TempPath"/>
		/// </summary>
		string InstanceJSONPath { get { return Path.Combine(TempPath, InstanceConfig.JSONFilename); } }

		/// <summary>
		/// Creates a default <see cref="InstanceConfig"/> at <see cref="TempPath"/>
		/// </summary>
		/// <returns></returns>
		IInstanceConfig CreateTempConfig()
		{
			return new InstanceConfig(TempPath);
		}

		/// <summary>
		/// Test that <see cref="InstanceConfig(string)"/> can execute successfully and doesn't automatically save
		/// </summary>
		[TestMethod]
		public void TestCreate()
		{
			var IC = CreateTempConfig();
			Assert.IsFalse(File.Exists(InstanceJSONPath));
		}

		/// <summary>
		/// Test that <see cref="InstanceConfig.Save"/> works correctly
		/// </summary>
		[TestMethod]
		public void TestSave()
		{
			var IC = CreateTempConfig();
			IC.Save();
			Assert.IsTrue(File.Exists(InstanceJSONPath));
		}

		/// <summary>
		/// Test that <see cref="InstanceConfig.Load(string)"/> works correctly
		/// </summary>
		[TestMethod]
		public void TestLoad()
		{
			var IC = CreateTempConfig();
			var name = "asdf";
			IC.Name = name;
			IC.Save();
			var IC2 = InstanceConfig.Load(TempPath);
			Assert.AreEqual(name, IC2.Name);
		}
	}
}
