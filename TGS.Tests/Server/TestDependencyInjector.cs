using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TGS.Server.Tests
{
	/// <summary>
	/// Unit tests for <see cref="Instance"/>
	/// </summary>
	[TestClass]
	public class TestDependencyInjector
	{
		[TestMethod]
		public void TestValidation()
		{
			var DI = new DependencyInjector();
		}
	}
}
