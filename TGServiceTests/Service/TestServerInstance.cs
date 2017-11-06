using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGServiceTests;

namespace TGServerService.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerInstance"/>
	/// </summary>
	[TestClass]
	public class TestServerInstance : TempDirectoryRequiredTest
	{
		/// <summary>
		/// Test a <see cref="ServerInstance"/> can be created and destroyed successfully with a basic <see cref="InstanceConfig"/>
		/// </summary>
		[TestMethod]
		public void TestBasicInstantiation()
		{
			new ServerInstance(new InstanceConfig(TempPath), 1).Dispose();
		}
	}
}
