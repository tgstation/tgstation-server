using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="ServiceRunner"/>
	/// </summary>
	[TestClass]
	public sealed class TestServiceRunner
	{
		[TestMethod]
		public void TestNullRun()
		{
			Assert.ThrowsException<ArgumentException>(() => new ServiceRunner().Run(null));
		}
	}
}
