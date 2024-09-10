using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Api;
using Tgstation.Server.Client;
using Tgstation.Server.Common;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Migrator.Properties;

using FileMode = System.IO.FileMode;

[DoesNotReturn]
static void ExitPause(int exitCode)
{
	Console.WriteLine("Consider saving the console text to report issues. Press any key to exit...");
	Console.ReadKey();
	Environment.Exit(exitCode);
}

try
{
	var commandLine = Environment.GetCommandLineArgs();
	var commandLineArguments = commandLine.Skip(1);
	var skipPreamble = commandLineArguments.Any(x => x.Equals("--skip-preamble", StringComparison.OrdinalIgnoreCase));

	Console.WriteLine("This is a very straightfoward script to migrate the instances of a TGS3 install into a new TGS6 install");

	static bool PromptYesOrNo(string question)
	{
		Console.Write($"{question} (y/n):");
		var character = Console.ReadKey();
		Console.WriteLine();
		return character.KeyChar.ToString().ToUpperInvariant() == "Y";
	}

	// WORKING DIRECTORY CHECK
	var currentAssembly = Assembly.GetExecutingAssembly();
	if(Path.GetDirectoryName(Path.GetFullPath(currentAssembly.Location))!.Replace("\\", "/").ToUpperInvariant()
		!= Path.GetFullPath(Environment.CurrentDirectory).Replace("\\", "/").ToUpperInvariant())
	{
		Console.WriteLine("Please keep the working directory equivalent to the program directory for this migration!");
		ExitPause(8);
	}

	// PREREQUISITE CHECK
	if (!skipPreamble)
	{
		Console.WriteLine("We need to ensure you're running this program as Administrator because there are several operations we'll do that require it.");
		Console.WriteLine("If not, you will be prompted to elevate this process.");
	}

	// ADMINISTRATOR CHECK
	static bool IsAdministrator()
	{
		using var identity = WindowsIdentity.GetCurrent();
		var principal = new WindowsPrincipal(identity);
		return principal.IsInRole(WindowsBuiltInRole.Administrator);
	}

	if (!IsAdministrator())
	{
		Console.WriteLine("Not running as admin. Elevating process...");
		var selfExecutable = commandLine.First().Replace(".dll", ".exe");
		var selfArguments = String.Join(" ", commandLineArguments) + " --skip-preamble";
		using var elevatedProcess = new Process();
		elevatedProcess.StartInfo.UseShellExecute = true;
		elevatedProcess.StartInfo.FileName = selfExecutable;
		elevatedProcess.StartInfo.Arguments = selfArguments;
		elevatedProcess.StartInfo.Verb = "runas";

		elevatedProcess.Start();
		return;
	}

	Console.WriteLine("Administrative privileges confirmed.");

	// TGS3 SERVICE CHECK

	const string PathToCommsBinary =
#if DEBUG
		"../../../../../Tgstation.Server.Migrator.Comms/bin/Debug/net472/win-x86/" +
#else
		"Comms/" +
#endif
		"Tgstation.Server.Migrator.Comms.exe";

	Console.WriteLine($"Checking {PathToCommsBinary} exists...");
	if (!File.Exists(PathToCommsBinary))
	{
		Console.WriteLine("Could not find WCF comms binary!");
		ExitPause(7);
	}

	static int RunComms(string command)
	{
		using var commsProcess = new Process();
		commsProcess.StartInfo.FileName = PathToCommsBinary;
		commsProcess.StartInfo.Arguments = command;
		commsProcess.Start();
		commsProcess.WaitForExit();
		return commsProcess.ExitCode;
	}

	Console.WriteLine("Checking for TGS3 service...");
	const string OldServiceName = "TG Station Server";
	const string NewServiceName = Constants.CanonicalPackageName;

	static ServiceController GetTgs3Service(bool checkNewOneIsntInstalled)
	{
		var allServices = ServiceController.GetServices();
		var tgs3Service = allServices.FirstOrDefault(service => service.ServiceName == OldServiceName);
		foreach (var service in allServices)
		{
			if (service == tgs3Service)
				continue;

			using (service)
				if (checkNewOneIsntInstalled && (service.ServiceName == NewServiceName || service.ServiceName == "tgstation-server-4"))
				{
					Console.WriteLine("Detected existing TGS4+ install! Cannot continue. Please uninstall any versions of TGS4+ before continuing.");
					ExitPause(10);
				}
		}

		if (checkNewOneIsntInstalled)
			Console.WriteLine("TGS4+ service install not detected.");

		if (tgs3Service == null)
		{
			Console.WriteLine("TGS3 is not installed on this machine!");
			ExitPause(5);
		}

		return tgs3Service;
	}

	using var tgs3Service = GetTgs3Service(true);

	if (tgs3Service.Status != ServiceControllerStatus.Running)
	{
		Console.WriteLine("TGS3 service is installed but not running! Please start the service before continuing.");
		ExitPause(9);
	}

	// TGS3 CONNECTION CHECK
	Console.WriteLine("Checking TGS3 connection...");
	var commsExitCode = RunComms("--verify-connection");
	if(commsExitCode != 0)
	{
		Console.WriteLine("Could not connect to TGS3 as administrator!");
		ExitPause(6);
	}

	// USER INPUT
	Console.WriteLine("We've confirmed you have have both TGS3 installed and TGS4+ service UNinstalled on THIS machine.");
	Console.WriteLine();
	Console.WriteLine("Please read all of the following CAREFULLY before proceeding:");
	Console.WriteLine("Confirm you want to migrate to the latest version installing the necessary prerequisite .NET version along the way.");
	Console.WriteLine("Please note that this is a one way upgrade and will not keep your DreamDaemon servers running throughout it.");
	Console.WriteLine("All TGS3 instances will be migrated in place. The following components will be preserved:");
	Console.WriteLine("- Repository (No test merge data or SSH key)");
	Console.WriteLine("- BYOND version (redownloaded from byond.com)");
	Console.WriteLine("- A FEW server configuration settings (Committer info, Autostart, Webclient, Game Port, Security Level)");
	Console.WriteLine("- EventHandlers");
	Console.WriteLine("- Chat Bots, if enabled");
	Console.WriteLine("  - TGS4+ doesn't support individual user/group identification. Admin channels will be used instead");
	Console.WriteLine("  - IRC authentication information cannot be copied and must be manually adjusted");
	Console.WriteLine("- Static Files");
	Console.WriteLine("- Code Modifications");
	Console.WriteLine("Remaining components such as logins, game builds, etc. can be recreated once the migration is complete.");
	Console.WriteLine("IMPORTANT NOTES:");
	Console.WriteLine("- INSTANCES CANNOT HAVE GAME PORTS OFFSET 111 UNITS FROM EACH OTHER OR HIGHER THAN 65423! WE AREN'T CORRECTING FOR THIS WHILE MIGRATING!");
	Console.WriteLine("- DISABLED INSTANCES WILL NOT BE MIGRATED! PLEASE ENABLE ALL INSTANCES YOU WISH TO MIGRATE BEFORE CONTINUING!");
	Console.WriteLine("- INSTANCE AUTO UPDATE CAN INTERFERE WITH THE MIGRATION! PLEASE DISABLE IT ON ALL INSTANCES BEING MIGRATED BEFORE CONTINUING!");
	Console.WriteLine("- DO NOT ATTEMPT TO USE TGS3 VIA NORMAL METHODS WHILE THIS MIGRATION IS TAKING PLACE OR YOU COULD CORRUPT YOUR DATA!");
	Console.WriteLine("Side note: You can skip the TGS6 setup wizard step by copying your premade appsettings.Production.yml file next to this .exe NOW.");
	if (!PromptYesOrNo("Proceed with upgrade?"))
	{
		Console.WriteLine("Prerequisite not met.");
		ExitPause(0);
	}

	string? tgsInstallPath = null;
	do
	{
		Console.WriteLine("Please enter the directory where you would like the TGS binaries installed.");
		Console.Write("This may be anywhere but should be empty: ");
		tgsInstallPath = Console.ReadLine();
		if (!String.IsNullOrWhiteSpace(tgsInstallPath))
		{
			if (!Path.IsPathRooted(tgsInstallPath))
			{
				Console.WriteLine("Please do not use a relative path for this. Enter the full path including the drive letter.");
				tgsInstallPath = null;
			}
			else if (Path.GetInvalidPathChars().Any(invalidChar => tgsInstallPath.Contains(invalidChar)))
			{
				Console.WriteLine("Invalid characters detected!");
				tgsInstallPath = null;
			}
		}
	}
	while (String.IsNullOrWhiteSpace(tgsInstallPath));

	Console.WriteLine("Attempting to create TGS install directory...");
	Directory.CreateDirectory(tgsInstallPath);

	// ASP.NET 8.0 RUNTIME CHECK
	Console.WriteLine("Next step, we need to ensure the ASP.NET Core 6 runtime is installed on your machine.");
	Console.WriteLine("We're going to download it for you.");
	Console.WriteLine("Yes, this program runs .NET 6, but it contains the entire runtime embedded into it. You will need a system-wide install for TGS.");

	var runtimeInstalled = true; // assume for now
	using (var dotnetRuntimeCheck = new Process())
	{
		dotnetRuntimeCheck.StartInfo.FileName = "C:/Program Files/dotnet/dotnet.exe";
		dotnetRuntimeCheck.StartInfo.Arguments = "--list-runtimes";
		dotnetRuntimeCheck.StartInfo.RedirectStandardOutput = true;

		try
		{
			dotnetRuntimeCheck.Start();

			dotnetRuntimeCheck.WaitForExit();
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
		{
			Console.WriteLine("Dotnet does not appear to be installed at all.");
			runtimeInstalled = false;
		}

		if (runtimeInstalled)
		{
			var versions = await dotnetRuntimeCheck.StandardOutput.ReadToEndAsync();
			var regex = new Regex("Microsoft\\.AspNetCore\\.App 8\\.0\\.[0-9]+");

			if (!regex.IsMatch(versions))
				runtimeInstalled = false;
		}
	}

	// ASP.NET 8.0 RUNTIME SETUP
	var assemblyName = currentAssembly.GetName();
	var productInfoHeaderValue =
		new ProductInfoHeaderValue(
			assemblyName.Name!,
			assemblyName.Version!.Semver().ToString());
	var httpClientFactory = new HttpClientFactory(productInfoHeaderValue);
	if (!runtimeInstalled)
	{
		// RUNTIME DONWLOAD
		Console.WriteLine("The version we are installing is the latest circa 26-09-2022, feel free to update it later if you want but that is not necessary.");

		var x64 = Environment.Is64BitOperatingSystem;
		var xSubstitution = x64 ? "64" : "86";
		Console.WriteLine($"Running on an x{xSubstitution} system.");

		var downloadUri = RuntimeDistributableAttribute.Instance.RuntimeDistributableUrl;

		var dotnetDownloadFilePath = $"dotnet-hosting-bundle-installer.exe";

		Console.WriteLine($"Downloading {downloadUri} to {Path.GetFullPath(dotnetDownloadFilePath)}...");

		using var httpClient = httpClientFactory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);
		var webRequestTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);
		using var response = await webRequestTask;
		response.EnsureSuccessStatusCode();
		await using (var responseStream = await response.Content.ReadAsStreamAsync())
		{
			await using var fileStream = new FileStream(
				dotnetDownloadFilePath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.ReadWrite | FileShare.Delete,
				4096,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
			await responseStream.CopyToAsync(fileStream);
		}

		// RUNTIME INSTALLATION
		Console.WriteLine("Runtime downloaded. Running silent installation...");
		bool silentInstallSuccess = true;
		using var silentInstallProcess = new Process();
		{
			silentInstallProcess.StartInfo.UseShellExecute = false;
			silentInstallProcess.StartInfo.FileName = dotnetDownloadFilePath;
			silentInstallProcess.StartInfo.Arguments = "/install /quiet /norestart";
			silentInstallProcess.Start();
			silentInstallProcess.WaitForExit();

			if (silentInstallProcess.ExitCode != 0)
			{
				Console.WriteLine("Silent installation failed! Please install the runtime interactively.");
				Console.WriteLine("Launching install dialog");
				silentInstallSuccess = false;
			}
		}

		if (!silentInstallSuccess)
		{
			using var installProcess = new Process();
			installProcess.StartInfo.FileName = dotnetDownloadFilePath;
			installProcess.Start();
			installProcess.WaitForExit();

			if (!PromptYesOrNo("Was the installation successful?"))
			{
				Console.WriteLine("Cannot continue without ASP.NET 8.0 runtime installed.");
				ExitPause(2);
			}
		}
	}
	else
	{
		Console.WriteLine("Runtime detected successfully. Continuing...");
	}


	// TGS6 ONLINE LOCATING
	Console.WriteLine("Now we're going to locate the latest version of the TGS service.");
	Console.WriteLine("(This migrator does not support the console runner, but you may switch the installation to it after completion)");

	Console.WriteLine("Determining latest version of TGS 5.X.X...");

	var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue(productInfoHeaderValue.Product!.Name, productInfoHeaderValue.Product.Version));

	string? gitHubPat = Environment.GetEnvironmentVariable("TGS_MIGRATOR_GITHUB_PAT");
	if (gitHubPat != null)
		gitHubClient.Credentials = new Credentials(gitHubPat);

	const int TgstationServerRepoId = 92952846;

	var allReleases = await gitHubClient.Repository.Release.GetAll(TgstationServerRepoId);

	const string VersionFiveTagPrefix = "tgstation-server-v5.";
	var allVersionFiveReleases = allReleases
		.Where(release => release.TagName.StartsWith(VersionFiveTagPrefix));
	var latestVersionFiveRelease = allVersionFiveReleases
		.OrderByDescending(release => Version.Parse(release.TagName[(VersionFiveTagPrefix.Length - 2)..]))
		.FirstOrDefault();

	if (latestVersionFiveRelease == null)
	{
		Console.WriteLine("Unable to determine latest version 5 release!");
		ExitPause(3);
	}

	Console.WriteLine($"Latest V5 version: {latestVersionFiveRelease.TagName}");

	var serverServiceAsset = latestVersionFiveRelease.Assets.FirstOrDefault(asset => asset.Name == "ServerService.zip");
	if (serverServiceAsset == null)
	{
		Console.WriteLine("Unable to determine ServerService.zip release asset!");
		ExitPause(4);
	}

	// TGS6 SETUP WIZARD
	Console.WriteLine("We are now going to run the TGS setup wizard to generate your new server configuration file.");

	var serverFactory = Tgstation.Server.Host.Core.Application.CreateDefaultServerFactory();
	_ = await serverFactory.CreateServer(new[] { $"General:SetupWizardMode={SetupWizardMode.Only}" }, null, default); // This is where the wizard actually runs

	// TGS6 DOWNLOAD AND UNZIP
	Console.WriteLine("Downloading TGS6...");

	using (var loggerFactory = LoggerFactory.Create(builder => { }))
	{
		BufferedFileStreamProvider tgsFiveZipBuffer;
		{
			var fileDownloader = new FileDownloader(httpClientFactory, loggerFactory.CreateLogger<FileDownloader>());
			await using var tgsFiveZipDownload = fileDownloader.DownloadFile(new Uri(serverServiceAsset.BrowserDownloadUrl), null);
			tgsFiveZipBuffer = new BufferedFileStreamProvider(
				await tgsFiveZipDownload.GetResult(default));
		}

		await using (tgsFiveZipBuffer)
		{
			Console.WriteLine("Unzipping TGS6...");
			await serverFactory.IOManager.ZipToDirectory(
				tgsInstallPath,
				await tgsFiveZipBuffer.GetResult(default),
				default);
		}
	}

	// TGS6 CONFIG SETUP
	const string ConfigurationFileName = "appsettings.Production.yml";
	Console.WriteLine("Extracting API port from configuration...");
	ushort configuredApiPort;
	{
		var configFileContents = await File.ReadAllTextAsync(ConfigurationFileName);
		var match = Regex.Match(configFileContents, "ApiPort: ([0-9]+)");
		if (!match.Success)
		{
			Console.WriteLine("Unable to extract ApiPort setting!");
			ExitPause(12);
		}

		configuredApiPort = ushort.Parse(match.Groups[1].Value);
	}

	Console.WriteLine("Moving configuration file from setup wizard to installation folder...");
	File.Copy(ConfigurationFileName, Path.Combine(tgsInstallPath, ConfigurationFileName));

	// TGS6 SERVICE SETUP
	Console.WriteLine("Installing TGS6 service...");
	using (var processInstaller = new ServiceProcessInstaller())
	using (var installer = new ServiceInstaller())
	{
		processInstaller.Account = ServiceAccount.LocalSystem;

		installer.Context = new InstallContext(
			"tgs-migrate-install.log",
			new string[]
			{
				$"assemblypath={Path.Combine(tgsInstallPath, "Tgstation.Server.Host.Service.exe")}"
			});
		installer.Description = "/tg/station 13 server running as a windows service";
		installer.DisplayName = "tgstation-server";
		installer.StartType = ServiceStartMode.Automatic;
		installer.ServicesDependedOn = new string[] { "Tcpip", "Dhcp", "Dnscache" };
		installer.ServiceName = NewServiceName;
		installer.Parent = processInstaller;

		var state = new ListDictionary();
		installer.Install(state);
	}

	Console.WriteLine("Starting TGS6 service...");
	var allServices = ServiceController.GetServices();
	using (var TGS6Service = allServices.FirstOrDefault(service => service.ServiceName == NewServiceName))
	{
		if (TGS6Service == null)
		{
			Console.WriteLine("Unable to locate newly installed TGS6 service!");
			ExitPause(11);
		}

		foreach (var service in allServices)
		{
			if (service == TGS6Service)
				continue;

			service.Dispose();
		}

		TGS6Service.Start();
		TGS6Service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(2));
	}

	// TGS6 CLIENT CONNECTION
	const int MaxWaitMinutes = 5;
	Console.WriteLine($"Connecting to TGS6 (Max {MaxWaitMinutes} minute wait)...");
	var giveUpAt = DateTimeOffset.UtcNow.AddMinutes(MaxWaitMinutes);

	var serverUrl = new Uri($"http://localhost:{configuredApiPort}");
	var clientFactory = new RestServerClientFactory(productInfoHeaderValue.Product);
	IRestServerClient TGS6Client;
	for (var I = 1; ; ++I)
	{
		try
		{
			Console.WriteLine($"Attempt {I}...");
			TGS6Client = await clientFactory.CreateFromLogin(
				serverUrl,
				DefaultCredentials.AdminUserName,
				DefaultCredentials.DefaultAdminUserPassword);
			break;
		}
		catch (HttpRequestException)
		{
			//migrating, to be expected
			if (DateTimeOffset.UtcNow > giveUpAt)
				throw;
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
		catch (ServiceUnavailableException)
		{
			// migrating, to be expected
			if (DateTimeOffset.UtcNow > giveUpAt)
				throw;
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
	}

	Console.WriteLine("Successfully connected to TGS6!");

	// COMMS MIGRATION
	Console.WriteLine("Deferring to Comms binary to migrate instances...");

	commsExitCode = RunComms($"--migrate {configuredApiPort}");
	if (commsExitCode != 0)
	{
		Console.WriteLine("Could not connect to TGS3 as administrator!");
		ExitPause(commsExitCode);
	}

	// TGS3 SHUTDOWN
	Console.WriteLine("Shutting down TGS3 service...");
	tgs3Service.Stop();
	tgs3Service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(2));
	tgs3Service.Dispose();

	Console.WriteLine("Disabling TGS3 service...");
	using (var managementObject = new ManagementObject(string.Format("Win32_Service.Name=\"{0}\"", OldServiceName)))
	{
		managementObject.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
	}

	if(tgs3Service.StartType != ServiceStartMode.Disabled)
		Console.WriteLine("Failed to disable TGS3 service! This isn't critical, however.");

	Console.WriteLine("Migration complete! Please continue uninstall TGS3 using Add/Remove Programs.");
	Console.WriteLine("Then configure TGS6 using an interactive client to build and start your server.");
	ExitPause(0);
}
catch (Exception ex)
{
	Console.WriteLine("An error occurred in the migration!");
	Console.WriteLine(ex);
	ExitPause(1);
}
