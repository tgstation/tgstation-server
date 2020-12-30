using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tgstation.Server.Api;
using Tgstation.Server.Client;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Tests
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	public sealed class VersionsTest
	{
		XNamespace xmlNamespace;

		XElement versionsPropertyGroup;

		[TestInitialize]
		public void Init()
		{
			var doc = XDocument.Load("../../../../../build/Version.props");
			var project = doc.Root;
			xmlNamespace = project.GetDefaultNamespace();
			versionsPropertyGroup = project.Elements().First(x => x.Name == xmlNamespace + "PropertyGroup");
			Assert.IsNotNull(versionsPropertyGroup);
		}

		[TestMethod]
		public void TestCoreVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsCoreVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(Program).Assembly.GetName().Version;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestConfigVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsConfigVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = GeneralConfiguration.CurrentConfigVersion;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestApiVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsApiVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(ApiHeaders).Assembly.GetName().Version;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestClientVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsClientVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(ServerClientFactory).Assembly.GetName().Version;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestWatchdogVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsHostWatchdogVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(Host.Watchdog.WatchdogFactory).Assembly.GetName().Version;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestDmapiVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsDmapiVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var lines = File.ReadAllLines("../../../../../src/DMAPI/tgs.dm");

			const string Prefix = "#define TGS_DMAPI_VERSION ";
			var versionLine = lines.FirstOrDefault(l => l.StartsWith(Prefix));
			Assert.IsNotNull(versionLine);

			versionLine = versionLine.Substring(Prefix.Length + 1, expected.ToString().Length);

			Assert.IsTrue(Version.TryParse(versionLine, out var actual));
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(expected, DMApiConstants.Version);
		}

		[TestMethod]
		public void TestControlPanelVersion()
		{
			var doc = XDocument.Load("../../../../../build/ControlPanelVersion.props");
			var project = doc.Root;
			var controlPanelXmlNamespace = project.GetDefaultNamespace();
			var controlPanelVersionsPropertyGroup = project.Elements().First(x => x.Name == controlPanelXmlNamespace + "PropertyGroup");
			var versionString = controlPanelVersionsPropertyGroup.Element(controlPanelXmlNamespace + "TgsControlPanelVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));

			var jsonText = File.ReadAllText("../../../../../src/Tgstation.Server.Host/ClientApp/package.json");

			dynamic json = JObject.Parse(jsonText);

			string cpVersionString = json.version;

			Assert.IsTrue(Version.TryParse(cpVersionString, out var actual));
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestWatchdogClientVersion()
		{
			var expected = typeof(Host.Watchdog.WatchdogFactory).Assembly.GetName().Version;
			var actual = Program.HostWatchdogVersion;
			Assert.AreEqual(expected.Major, actual.Major);
			Assert.AreEqual(expected.Minor, actual.Minor);
			Assert.AreEqual(expected.Build, actual.Build);
			Assert.AreEqual(-1, actual.Revision);
		}

		[TestMethod]
		public async Task TestContainerScriptVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsContainerScriptVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var scriptLines = await File.ReadAllLinesAsync("../../../../../build/tgs.docker.sh");

			var line = scriptLines.FirstOrDefault(x => x.Trim().Contains($"SCRIPT_VERSION=\"{expected.Semver()}\""));
			Assert.IsNotNull(line);
		}

		[TestMethod]
		public void TestDowngradeMigrations()
		{
			static string GetMigrationTimestampString(Type type) => type
				?.GetCustomAttributes(typeof(MigrationAttribute), false)
				.OfType<MigrationAttribute>()
				.SingleOrDefault()
				?.Id
				.Split('_')
				.First()
				?? String.Empty;

			var allTypesWithMigrationAttributes = typeof(Program)
				.Assembly
				.GetTypes()
				.ToDictionary(
					x => x,
					x => GetMigrationTimestampString(x));

			Type latestMigrationMS = null;
			Type latestMigrationMY = null;
			Type latestMigrationPG = null;
			Type latestMigrationSL = null;
			foreach (var kvp in allTypesWithMigrationAttributes)
			{
				var migrationType = kvp.Key;
				var migrationTimestamp = kvp.Value;

				switch(migrationType.Name.Substring(0, 2))
				{
					case "MS":
						if (String.Compare(GetMigrationTimestampString(latestMigrationMS), migrationTimestamp) < 0)
							latestMigrationMS = migrationType;
						break;
					case "MY":
						if (String.Compare(GetMigrationTimestampString(latestMigrationMY), migrationTimestamp) < 0)
							latestMigrationMY = migrationType;
						break;
					case "PG":
						if (String.Compare(GetMigrationTimestampString(latestMigrationPG), migrationTimestamp) < 0)
							latestMigrationPG = migrationType;
						break;
					case "SL":
						if (String.Compare(GetMigrationTimestampString(latestMigrationSL), migrationTimestamp) < 0)
							latestMigrationSL = migrationType;
						break;
				}
			}

			Assert.AreEqual(latestMigrationMS, DatabaseContext.MSLatestMigration);
			Assert.AreEqual(latestMigrationMY, DatabaseContext.MYLatestMigration);
			Assert.AreEqual(latestMigrationPG, DatabaseContext.PGLatestMigration);
			Assert.AreEqual(latestMigrationSL, DatabaseContext.SLLatestMigration);
		}
	}
}
