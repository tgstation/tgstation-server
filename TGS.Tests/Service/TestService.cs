using Microsoft.VisualStudio.TestTools.UnitTesting;
using TGServiceTests;

namespace TGS.Server.Service.Tests
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
		/// Starts and stops a <see cref="Service"/>
		/// </summary>
		void StartStopServiceBasic()
		{
			using (var S = new ServiceAccessor())
			{
				S.FakeStart(new string[] { });
				S.FakeStop();
			}
		}

		/// <summary>
		/// Test <see cref="Service.OnStart(string[])"/> and <see cref="Service.OnStop"/> can execute successfully
		/// </summary>
		[TestMethod]
		public void TestStartupAndShutdown()
		{
			StartStopServiceBasic();
		}
		
		/// <summary>
		/// Test <see cref="Service.OnStart(string[])"/> and <see cref="Service.OnStop"/> can execute successfully with a commandline port override
		/// </summary>
		[TestMethod]
		public void TestCommandLinePortSet()
		{
			Properties.Settings.Default.RemoteAccessPort = 11111;
			using (var S = new ServiceAccessor())
			{
				S.FakeStart(new string[] { "-port", "36785" });
				S.FakeStop();
			}
			Assert.AreEqual(Properties.Settings.Default.RemoteAccessPort, 36785);
		}

		/// <summary>
		/// Test that the .NET config is always initialized regardless of <see cref="Properties.Settings.UpgradeRequired"/>
		/// </summary>
		[TestMethod]
		public void TestNETConfigIsAlwaysPrepped()
		{
			Properties.Settings.Default.UpgradeRequired = true;
			Properties.Settings.Default.InstancePaths = null;
			StartStopServiceBasic();
			Assert.IsNotNull(Properties.Settings.Default.InstancePaths);
			Properties.Settings.Default.UpgradeRequired = false;
			Properties.Settings.Default.InstancePaths = null;
			StartStopServiceBasic();
			Assert.IsNotNull(Properties.Settings.Default.InstancePaths);
		}
	}
}
