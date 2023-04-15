using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Host.Components.Interop;

namespace Tgstation.Server.Tests
{
	[TestClass]
	public sealed class DMApiConstantsTests
	{
		string[] definesFileLines;

		[TestInitialize]
		public async Task Initialize()
		{
			definesFileLines = await File.ReadAllLinesAsync("../../../../../src/DMAPI/tgs/v5/_defines.dm");
		}

		[TestMethod]
		public void TestDMApiConstants()
		{
			// we only test a few things because they are sourced by BYOND and we want to validate them
			CheckLine("DMAPI5_BRIDGE_REQUEST_LIMIT", DMApiConstants.MaximumBridgeRequestLength);
		}

		void CheckLine(string defineName, object expectedValue)
		{
			var line = definesFileLines.FirstOrDefault(x => x.Contains(defineName));
			Assert.IsNotNull(line, $"Missing {defineName} define!");
			var splits = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
			var defineDefinitionIndex = splits.IndexOf(defineName) + 1;
			Assert.AreNotEqual(-1, defineDefinitionIndex);
			var defineDefinition = String.Concat(splits.Skip(defineDefinitionIndex));
			Assert.AreEqual(expectedValue.ToString(), defineDefinition, $"Wrong value for {defineName}!");
		}
	}
}
