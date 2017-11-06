using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGServiceTests;

namespace TGServerService.Tests
{
	/// <summary>
	/// Tests for <see cref="Service"/>
	/// </summary>
	[TestClass]
	public class TestService : TempDirectoryRequiredTest
	{
		/// <summary>
		/// Test <see cref="Service.Service"/> can execute successfully
		/// </summary>
		[TestMethod]
		public void TestInstantiation()
		{
			new Service().Dispose();
		}

		/// <summary>
		/// Test <see cref="Service.OnStart(string[])"/> and <see cref="Service.OnStop"/> can execute successfully
		/// </summary>
		[TestMethod]
		public void TestStartupAndShutdown()
		{
			using (var S = new ServiceAccessor())
			{
				S.FakeStart(new string[] { });
				S.FakeStop();
			}
		}
		
		/// <summary>
		/// Test <see cref="Service.OnStart(string[])"/> and <see cref="Service.OnStop"/> can execute successfully with a commandline port override
		/// </summary>
		[TestMethod]
		public void TestCommandLinePortSet()
		{
			using (var S = new ServiceAccessor())
			{
				S.FakeStart(new string[] { "-port", "36785" });
				S.FakeStop();
			}
		}
	}
}
