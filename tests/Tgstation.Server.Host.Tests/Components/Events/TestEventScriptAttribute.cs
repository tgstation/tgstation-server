using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;


namespace Tgstation.Server.Host.Components.Events.Tests
{
	/// <summary>
	/// Tests for the <see cref="EventScriptAttribute"/> <see langword="class"/>.
	/// </summary>
	[TestClass]
	public sealed class TestEventScriptAttribute
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new EventScriptAttribute(null));
			var test = new EventScriptAttribute("test");
			Assert.AreEqual("test", test.ScriptName);
		}
	}
}
