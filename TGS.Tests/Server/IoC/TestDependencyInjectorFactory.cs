using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TGS.Server.IoC.Tests
{
	/// <summary>
	/// Tests for <see cref="DependencyInjectorFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestDependencyInjectorFactory
	{
		[TestMethod]
		public void TestFactory()
		{
			var DIF = new DependencyInjectorFactory();
			Assert.IsNotNull(DIF.CreateDependencyInjector());
		}
	}
}
