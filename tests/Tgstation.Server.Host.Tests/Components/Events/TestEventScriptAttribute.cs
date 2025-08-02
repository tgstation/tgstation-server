using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;


namespace Tgstation.Server.Host.Components.Events.Tests
{
	/// <summary>
	/// Tests for the <see cref="EventScriptAttribute"/> class.
	/// </summary>
	[TestClass]
	public sealed class TestEventScriptAttribute
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new EventScriptAttribute(null));
			var test = new EventScriptAttribute("test1", "test2");
			Assert.IsTrue(test.ScriptNames.SequenceEqual(["test1", "test2"]));
		}
	}
}
