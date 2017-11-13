using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TGServiceTests;

namespace TGS.Server.Service.Tests
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

		/// <summary>
		/// Test the return value of <see cref="ServerInstance.PushTestmergeCommits"/>
		/// </summary>
		[TestMethod]
		public void TestPushTestMergeCommits()
		{
			var ic = new InstanceConfig(TempPath) { PushTestmergeCommits = false };
			using (var si = new ServerInstance(ic, 1))
			{
				Assert.IsFalse(si.PushTestmergeCommits());
				ic.PushTestmergeCommits = true;
				Assert.IsTrue(si.PushTestmergeCommits());
			}
		}

		[TestMethod]
		public void TestSetPushTestMergeCommits()
		{
			var ic = new InstanceConfig(TempPath) { PushTestmergeCommits = false };
			using (var si = new ServerInstance(ic, 1))
			{
				si.SetPushTestmergeCommits(true);
				Assert.IsTrue(ic.PushTestmergeCommits);
				si.SetPushTestmergeCommits(false);
				Assert.IsFalse(ic.PushTestmergeCommits);
			}
		}
	}
}
