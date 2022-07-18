using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tgstation.Server.Host.Jobs.Tests
{
	[TestClass]
	public sealed class TestJobException
	{
		[TestMethod]
		public void TestConstruction()
		{
			new JobException();
			new JobException("Message");
			new JobException("Message", new Exception("Inner Message"));
		}
	}
}
