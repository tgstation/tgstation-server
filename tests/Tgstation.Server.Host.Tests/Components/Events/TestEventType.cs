using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Host.Components.Events.Tests
{
	[TestClass]
	public sealed class TestEventType
	{
		[TestMethod]
		public void TestAllEventTypesHaveUniqueEventScriptAttributes()
		{
			var allScripts = new HashSet<string>();
			foreach (var eventType in Enum.GetValues(typeof(EventType))) {
				var list = typeof(EventType)
					.GetField(eventType.ToString())
					.GetCustomAttributes(false)
					.OfType<EventScriptAttribute>()
					.ToList();
				Assert.AreEqual(1, list.Count, $"EventType: {eventType}");

				var attribute = list.First();
				Assert.IsTrue(allScripts.Add(attribute.ScriptName), $"Non-unique script Name: {attribute.ScriptName}");
			}
   		}
	}
}
