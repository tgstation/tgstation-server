using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="ProjectInstaller"/>
	/// </summary>
	[TestClass]
	public sealed class TestProjectInstaller
	{
		[TestMethod]
		public void TestConstruction()
		{
			new ProjectInstaller();
		}
	}
}
