using System;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using Tgstation.Server.Api;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Tests.Live;
using Tgstation.Server.Host.Properties;

namespace Tgstation.Server.Tests
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	public sealed class TestVersions
	{
		static XNamespace xmlNamespace;

		static XElement versionsPropertyGroup;

		[ClassInitialize]
		public static void Init(TestContext _)
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
		public void TestRestVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsRestVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			Assert.AreEqual(expected, ApiHeaders.Version);
		}

		[TestMethod]
		public void TestGraphQLVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsGraphQLVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			Assert.AreEqual(expected, Version.Parse(MasterVersionsAttribute.Instance.RawGraphQLVersion));
		}

		[TestMethod]
		public void TestApiLibraryVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsApiLibraryVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(ApiHeaders).Assembly.GetName().Version;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public async Task TestDDExeByondVersion()
		{
			var mockGeneralConfigurationOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockGeneralConfigurationOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var mockSessionConfigurationOptions = new Mock<IOptionsMonitor<SessionConfiguration>>();
			mockSessionConfigurationOptions.SetupGet(x => x.CurrentValue).Returns(new SessionConfiguration());

			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
			var logger = loggerFactory.CreateLogger<CachingFileDownloader>();

			// windows only BYOND but can be checked on any system
			var init1 = CachingFileDownloader.InitializeByondVersion(
				logger,
				WindowsByondInstaller.DDExeVersion,
				true,
				CancellationToken.None);
			await CachingFileDownloader.InitializeByondVersion(
				logger,
				new Version(WindowsByondInstaller.DDExeVersion.Major, WindowsByondInstaller.DDExeVersion.Minor - 1),
				true,
				CancellationToken.None);
			await init1;

			using var byondInstaller = new WindowsByondInstaller(
				Mock.Of<IProcessExecutor>(),
				Mock.Of<IIOManager>(),
				new CachingFileDownloader(Mock.Of<ILogger<CachingFileDownloader>>()),
				mockGeneralConfigurationOptions.Object,
				mockSessionConfigurationOptions.Object,
				Mock.Of<ILogger<WindowsByondInstaller>>());

			const string ArchiveEntryPath = "byond/bin/dd.exe";
			var hasEntry = ArchiveHasFileEntry(
				await TestingUtils.ExtractMemoryStreamFromInstallationData(
					await byondInstaller.DownloadVersion(
						new EngineVersion
						{
							Engine = EngineType.Byond,
							Version = WindowsByondInstaller.DDExeVersion
						},
						null,
						default),
					CancellationToken.None),
				ArchiveEntryPath);

			Assert.IsTrue(hasEntry);

			var (byondBytes, _) = await GetByondVersionPriorTo(byondInstaller, WindowsByondInstaller.DDExeVersion);
			hasEntry = ArchiveHasFileEntry(
				byondBytes,
				ArchiveEntryPath);

			Assert.IsFalse(hasEntry);
		}

		static Version MapThreadsVersion() => (Version)typeof(ByondInstallerBase).GetField("MapThreadsVersion", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) ?? throw new InvalidOperationException("Couldn't find MapThreadsVersion");

		[TestMethod]
		public async Task TestMapThreadsByondVersion()
		{
			var mockGeneralConfigurationOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockGeneralConfigurationOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration
			{
				SkipAddingByondFirewallException = true,
			});
			var mockSessionConfigurationOptions = new Mock<IOptionsMonitor<SessionConfiguration>>();
			mockSessionConfigurationOptions.SetupGet(x => x.CurrentValue).Returns(new SessionConfiguration());

			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			var platformIdentifier = new PlatformIdentifier();
			var logger = loggerFactory.CreateLogger<CachingFileDownloader>();
			var init1 = CachingFileDownloader.InitializeByondVersion(
				logger,
				MapThreadsVersion(),
				platformIdentifier.IsWindows,
				CancellationToken.None);
			await CachingFileDownloader.InitializeByondVersion(
				logger,
				new Version(MapThreadsVersion().Major, MapThreadsVersion().Minor - 1),
				platformIdentifier.IsWindows,
				CancellationToken.None);
			await init1;

			var fileDownloader = new CachingFileDownloader(Mock.Of<ILogger<CachingFileDownloader>>());

			ByondInstallerBase byondInstaller = platformIdentifier.IsWindows
				? new WindowsByondInstaller(
					Mock.Of<IProcessExecutor>(),
					Mock.Of<IIOManager>(),
					fileDownloader,
					mockGeneralConfigurationOptions.Object,
					mockSessionConfigurationOptions.Object,
					loggerFactory.CreateLogger<WindowsByondInstaller>())
				: new PosixByondInstaller(
					new PosixPostWriteHandler(loggerFactory.CreateLogger<PosixPostWriteHandler>()),
					new DefaultIOManager(),
					fileDownloader,
					loggerFactory.CreateLogger<PosixByondInstaller>());
			using var disposable = byondInstaller as IDisposable;

			var processExecutor = new ProcessExecutor(
				platformIdentifier.IsWindows
					? new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
					: new PosixProcessFeatures(
						new Lazy<IProcessExecutor>(() => null),
						new DefaultIOManager(),
						loggerFactory.CreateLogger<PosixProcessFeatures>()),
					Mock.Of<IIOManager>(),
					loggerFactory.CreateLogger<ProcessExecutor>(),
					loggerFactory);

			var ioManager = new DefaultIOManager();
			var tempPath = ioManager.ConcatPath(LiveTestingServer.BaseDirectory, "mapthreads");
			await ioManager.CreateDirectory(tempPath, default);
			try
			{
				await TestMapThreadsVersion(
					new EngineVersion
					{
						Engine = EngineType.Byond,
						Version = MapThreadsVersion(),
					},
					await TestingUtils.ExtractMemoryStreamFromInstallationData(
						await byondInstaller.DownloadVersion(
							new EngineVersion
							{
								Engine = EngineType.Byond,
								Version = MapThreadsVersion()
							},
							null,
							default),
						CancellationToken.None),
					byondInstaller,
					ioManager,
					processExecutor,
					tempPath);

				await ioManager.DeleteDirectory(tempPath, default);

				var (byondBytes, version) = await GetByondVersionPriorTo(byondInstaller, MapThreadsVersion());

				await TestMapThreadsVersion(
					version,
					byondBytes,
					byondInstaller,
					ioManager,
					processExecutor,
					tempPath);
			}
			finally
			{
				await ioManager.DeleteDirectory(tempPath, default);
			}
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			CachingFileDownloader.Cleanup();
		}

		[TestMethod]
		public void TestClientVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsClientVersion").Value + ".0";
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			var actual = typeof(RestServerClientFactory).Assembly.GetName().Version;
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
		}

		[TestMethod]
		public void TestInteropVersion()
		{
			var versionString = versionsPropertyGroup.Element(xmlNamespace + "TgsInteropVersion").Value;
			Assert.IsNotNull(versionString);
			Assert.IsTrue(Version.TryParse(versionString, out var expected));
			Assert.AreEqual(expected, DMApiConstants.InteropVersion);
		}

		[TestMethod]
		public void TestControlPanelVersion()
		{
			var doc = XDocument.Load("../../../../../build/WebpanelVersion.props");
			var project = doc.Root;
			var controlPanelXmlNamespace = project.GetDefaultNamespace();
			var controlPanelVersionsPropertyGroup = project.Elements().First(x => x.Name == controlPanelXmlNamespace + "PropertyGroup");
			var versionString = controlPanelVersionsPropertyGroup.Element(controlPanelXmlNamespace + "TgsWebpanelVersion").Value;
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

				switch (migrationType.Name[..2])
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

		[TestMethod]
		public async Task CheckWebRootPathForTgsLogo()
		{
			var directory = Path.GetFullPath("../../../../../src/Tgstation.Server.Host/wwwroot");
			if (!Directory.Exists(directory))
				Assert.Inconclusive("Webpanel not built?");

			static string GetConstField(string name) => (string)typeof(RootController).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

			var logo = new PlatformIdentifier().IsWindows
				? GetConstField("LogoSvgWindowsName")
				: GetConstField("LogoSvgLinuxName");

			var path = $"../../../../../src/Tgstation.Server.Host/wwwroot/{logo}.svg";
			Assert.IsTrue(File.Exists(path));

			var content = await File.ReadAllBytesAsync(path);
			var hash = String.Join(String.Empty, SHA1.HashData(content).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
			Assert.AreEqual(
				new PlatformIdentifier().IsWindows
					? "c5e4709774c14a6f376dbb5100bd80a0114a2287"
					: "9eba2fac24c5c7e0008721690d07c3df575a00d6",
				hash);
		}

		static async Task<Tuple<Stream, EngineVersion>> GetByondVersionPriorTo(ByondInstallerBase byondInstaller, Version version)
		{
			var minusOneMinor = new Version(version.Major, version.Minor - 1);
			var byondVersion = new EngineVersion
			{
				Engine = EngineType.Byond,
				Version = minusOneMinor
			};
			try
			{
				return Tuple.Create(await TestingUtils.ExtractMemoryStreamFromInstallationData(await byondInstaller.DownloadVersion(
					byondVersion,
					null,
					CancellationToken.None), CancellationToken.None), byondVersion);
			}
			catch (HttpRequestException)
			{
				var minusOneMajor = new Version(minusOneMinor.Major - 1, minusOneMinor.Minor);
				byondVersion.Version = minusOneMajor;
				return Tuple.Create(await TestingUtils.ExtractMemoryStreamFromInstallationData(await byondInstaller.DownloadVersion(
					byondVersion,
					null,
					CancellationToken.None), CancellationToken.None), byondVersion);
			}
		}

		static async Task TestMapThreadsVersion(
			EngineVersion engineVersion,
			Stream byondBytes,
			ByondInstallerBase byondInstaller,
			DefaultIOManager ioManager,
			ProcessExecutor processExecutor,
			string tempPath)
		{
			using (byondBytes)
				await ioManager.ZipToDirectory(tempPath, byondBytes, default);

			// HAAAAAAAX
			var installerType = byondInstaller.GetType();
			if (byondInstaller is WindowsByondInstaller)
				typeof(WindowsByondInstaller).GetField("installedDirectX", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(byondInstaller, true);

			await byondInstaller.Install(engineVersion, tempPath, false, default);

			var binPath = (string)typeof(ByondInstallerBase).GetField("ByondBinPath", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			var ddNameFunc = installerType.GetMethod("GetDreamDaemonName", BindingFlags.Instance | BindingFlags.NonPublic);
			var supportsCli = false;
			var argArray = new object[] { engineVersion.Version, supportsCli };

			// https://stackoverflow.com/questions/2438065/how-can-i-invoke-a-method-with-an-out-parameter
			var ddPath = ioManager.ConcatPath(
				tempPath,
				binPath,
				(string)ddNameFunc.Invoke(byondInstaller, argArray));

			Assert.IsTrue((bool)argArray[1]);

			var shouldSupportMapThreads = engineVersion.Version >= MapThreadsVersion();

			await File.WriteAllBytesAsync("fake.dmb", [], CancellationToken.None);

			try
			{
				await using var process = await processExecutor.LaunchProcess(
					ddPath,
					Environment.CurrentDirectory,
					"fake.dmb -map-threads 3 -close",
					CancellationToken.None,
					null,
					null,
					true,
					true);

				try
				{
					await process.Startup;
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
					await process.Lifetime.WaitAsync(cts.Token);

					var output = await process.GetCombinedOutput(cts.Token);

					var supportsMapThreads = !output.Contains("invalid option '-map-threads'");
					Assert.AreEqual(shouldSupportMapThreads, supportsMapThreads, $"DD Output:{Environment.NewLine}{output}");
				}
				finally
				{
					process.Terminate();
				}
			}
			finally
			{
				File.Delete("fake.dmb");
			}
		}

		static bool ArchiveHasFileEntry(Stream byondBytes, string entryPath)
		{
			using (byondBytes)
			{
				using var archive = new ZipArchive(byondBytes, ZipArchiveMode.Read);

				var entry = archive.Entries.FirstOrDefault(entry => entry.FullName == entryPath);

				return entry != null;
			}
		}
	}
}
