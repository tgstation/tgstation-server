using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using TGS.Interface;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Extensions;

static class Program
{
	static async Task<int> Main(string[] args)
	{
		try
		{
			var tgs3Client = new Client();

			switch (args[0])
			{
				case "--verify-connection":
					var status = tgs3Client.ConnectionStatus(out var error);
					if (status != ConnectivityLevel.Administrator)
					{
						Console.WriteLine($"Connection Error: {error}");
						return 3;
					}
					return 0;
				case "--migrate":
					ushort apiPort = ushort.Parse(args[1]);
					return await Migrate(tgs3Client, apiPort);
				default:
					return 2;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return 1;
		}
	}

	static async Task<int> Migrate(IClient tgs3Client, ushort apiPort)
	{
#if DEBUG
		Console.WriteLine("Test log line...");
		System.Diagnostics.Debugger.Launch();
#endif
		Console.WriteLine("Connecting to TGS3...");
		var status = tgs3Client.ConnectionStatus(out var tgs3Error);
		if (status != ConnectivityLevel.Administrator)
		{
			Console.WriteLine($"Connection Error: {tgs3Client}");
			return 13;
		}

		Console.WriteLine("Connected!");

		Console.WriteLine("Connecting to TGS6...");
		var assemblyName = Assembly.GetExecutingAssembly().GetName();
		var productInfoHeaderValue =
			new ProductInfoHeaderValue(
				assemblyName.Name!,
				assemblyName.Version!.Semver().ToString());

		var serverUrl = new Uri($"http://localhost:{apiPort}");
		var clientFactory = new RestServerClientFactory(productInfoHeaderValue.Product);
		var TGS6Client = await clientFactory.CreateFromLogin(
			serverUrl,
			DefaultCredentials.AdminUserName,
			DefaultCredentials.DefaultAdminUserPassword);

		Console.WriteLine("Connected!");

		// we do this synchronously and patiently because we aren't chumbii and this is delicate
		// We need clear logs
		var tgs3Instances = tgs3Client.Server.Instances.ToList();
		foreach (var tgs3Instance in tgs3Instances)
		{
			var instanceName = tgs3Instance.Metadata.Name;
			var instancePath = tgs3Instance.Metadata.Path;

			if (!tgs3Instance.Metadata.Enabled)
			{
				Console.WriteLine($"Skipping instance {instanceName} at {instancePath}. Disabled.");
				continue;
			}

			Console.WriteLine($"Migrating instance {instanceName} at {instancePath}");

			RepositoryUpdateRequest? repositoryUpdateRequest = null;
			if (tgs3Instance.Repository.Exists())
			{
				Console.WriteLine("Gathering instance repository data...");
				repositoryUpdateRequest = new RepositoryUpdateRequest
				{
					CommitterEmail = tgs3Instance.Repository.GetCommitterEmail(),
					CommitterName = tgs3Instance.Repository.GetCommitterName(),
					UpdateSubmodules = true, // default in 3
					Reference = tgs3Instance.Repository.GetBranch(out tgs3Error),
				};

				if (tgs3Error != null)
				{
					Console.WriteLine($"Error retrieving current branch: {tgs3Error}");
				}
			}
			else
				Console.WriteLine("Instance has no repository, that's fine.");

			Console.WriteLine("Gather DreamDaemon and DreamMaker data...");
			var dreamDaemonRequest = new DreamDaemonRequest
			{
				AllowWebClient = tgs3Instance.DreamDaemon.Webclient(),
				AutoStart = tgs3Instance.DreamDaemon.Autostart(),
				Port = tgs3Instance.DreamDaemon.Port(),
				SecurityLevel = tgs3Instance.DreamDaemon.SecurityLevel() switch
				{
					TGS.Interface.DreamDaemonSecurity.Safe => Tgstation.Server.Api.Models.DreamDaemonSecurity.Safe,
					TGS.Interface.DreamDaemonSecurity.Ultrasafe => Tgstation.Server.Api.Models.DreamDaemonSecurity.Ultrasafe,
					_ => Tgstation.Server.Api.Models.DreamDaemonSecurity.Trusted,
				}
			};

			var dreamMakerRequest = new DreamMakerRequest
			{
				ApiValidationSecurityLevel = dreamDaemonRequest.SecurityLevel,
				ApiValidationPort = (ushort)(dreamDaemonRequest.Port + 111) // Best rotation we can do...
			};

			Console.WriteLine("Gathering chat data...");
			var providerInfos = tgs3Instance.Chat.ProviderInfos();
			var chatBotCreateRequests = new List<ChatBotCreateRequest>();
			foreach(var providerInfo in providerInfos)
			{
				if (!providerInfo.Enabled)
					continue;

				var createRequest = new ChatBotCreateRequest()
				{
					Provider = providerInfo.Provider switch
					{
						TGS.Interface.ChatProvider.Discord => Tgstation.Server.Api.Models.ChatProvider.Discord,
						_ => Tgstation.Server.Api.Models.ChatProvider.Irc,
					},
					Enabled = true,
					ReconnectionInterval = 5,
				};

				var isDiscordProvider = createRequest.Provider == Tgstation.Server.Api.Models.ChatProvider.Discord;
				createRequest.Name = isDiscordProvider
					? "Discord Bot"
					: "IRC Bot";

				Console.WriteLine($"Gathering data for {createRequest.Name}...");

				ChatConnectionStringBuilder csb;
				if (createRequest.Provider == Tgstation.Server.Api.Models.ChatProvider.Discord)
				{
					var discordSetupInfo = new DiscordSetupInfo(providerInfo);
					csb = new DiscordConnectionStringBuilder
					{
						DMOutputDisplay = DiscordDMOutputDisplayType.Always,
						BotToken = discordSetupInfo.BotToken
					};

				}
				else
				{
					var ircSetupInfo = new IRCSetupInfo(providerInfo);
					csb = new IrcConnectionStringBuilder
					{
						Address = ircSetupInfo.URL,
						Nickname = ircSetupInfo.Nickname,
						Port = ircSetupInfo.Port
					};
				}
				createRequest.ConnectionString = csb.ToString();

				createRequest.Channels = new List<ChatChannel>();

				static string NormalizeChannelId(string channelId) => channelId.ToLowerInvariant().Trim();

				var distinctChannels = providerInfo.WatchdogChannels
					.Union(providerInfo.DevChannels)
					.Union(providerInfo.AdminChannels)
					.Union(providerInfo.GameChannels)
					.Select(NormalizeChannelId)
					.Distinct();

				foreach(var channelIdentifier in distinctChannels)
				{
					var newChatChannel = new ChatChannel
					{
						IsWatchdogChannel = providerInfo.WatchdogChannels.Any(x => NormalizeChannelId(x) == channelIdentifier),
						IsAdminChannel = providerInfo.AdminChannels.Any(x => NormalizeChannelId(x) == channelIdentifier),
						IsUpdatesChannel = providerInfo.DevChannels.Any(x => NormalizeChannelId(x) == channelIdentifier),
						// system channels are too new a feature to target
						ChannelData = channelIdentifier,
					};

					createRequest.Channels.Add(newChatChannel);
				}

				chatBotCreateRequests.Add(createRequest);
			}

			Console.WriteLine("Detaching TGS3 instance...");
			tgs3Client.Server.InstanceManager.DetachInstance(instanceName);

			Console.WriteLine("Creating TGS6 attach file...");
			File.WriteAllText(Path.Combine(instancePath, "TGS4_ALLOW_INSTANCE_ATTACH"), String.Empty);

			Console.WriteLine("Checking BYOND install...");
			var byondDirectory = Path.Combine(instancePath, "BYOND");
			var byondVersionFile = Path.Combine(byondDirectory, "byond_version.dat");

			EngineVersionRequest? byondVersionRequest = null;
			if (Directory.Exists(byondDirectory) && File.Exists(byondVersionFile))
			{
				var byondVersion = Version.Parse(File.ReadAllText(byondVersionFile).Trim());
				Console.WriteLine($"Found installed BYOND version: {byondVersion.Major}.{byondVersion.Minor}");
				byondVersionRequest = new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = byondVersion
					}
				};
			}

			var oldStaticDirectory = Path.Combine(instancePath, "Static");
			var newConfigurationDirectory = Path.Combine(instancePath, "Configuration");
			if (Directory.Exists(oldStaticDirectory))
			{
				Console.WriteLine("Migrating Static to Configuration/GameStaticFiles");
				var gameStaticFilesDirectory = Path.Combine(newConfigurationDirectory, "GameStaticFiles");
				Directory.CreateDirectory(newConfigurationDirectory);
				Directory.Move(oldStaticDirectory, gameStaticFilesDirectory);

				Console.WriteLine("Moving code modifications...");
				var codeModsDirectory = Path.Combine(newConfigurationDirectory, "CodeModifications");
				Directory.CreateDirectory(codeModsDirectory);

				var allDmFiles = Directory.EnumerateFiles(gameStaticFilesDirectory, "*.dm", SearchOption.TopDirectoryOnly).ToList();
				foreach (var dmFile in allDmFiles)
				{
					File.Move(Path.Combine(gameStaticFilesDirectory, dmFile), Path.Combine(codeModsDirectory, Path.GetFileName(dmFile)));
				}

				var allDmeFiles = Directory.EnumerateFiles(gameStaticFilesDirectory, "*.dme", SearchOption.TopDirectoryOnly).ToList();
				if (allDmeFiles.Any())
				{
					foreach (var dmeFile in allDmeFiles)
					{
						File.Move(Path.Combine(gameStaticFilesDirectory, dmeFile), Path.Combine(codeModsDirectory, Path.GetFileName(dmeFile)));
					}
				}
				else if (allDmFiles.Any())
				{
					Console.WriteLine("Generating HeadInclude.dm...");
					var headIncludeBuilder = new StringBuilder();
					foreach (var dmFile in allDmFiles.OrderBy(fileName => fileName.ToUpperInvariant()))
					{
						headIncludeBuilder.Append("#include \"");
						headIncludeBuilder.Append(Path.GetFileName(dmFile));
						headIncludeBuilder.Append("\"");
						headIncludeBuilder.Append(Environment.NewLine);
					}

					File.WriteAllText(Path.Combine(codeModsDirectory, "HeadInclude.dm"), headIncludeBuilder.ToString());
				}
			}

			var eventHandlersDirectory = Path.Combine(instancePath, "EventHandlers");
			if (Directory.Exists(eventHandlersDirectory))
			{
				Console.WriteLine("Moving event scripts...");
				Directory.CreateDirectory(newConfigurationDirectory);
				Directory.Move(eventHandlersDirectory, Path.Combine(newConfigurationDirectory, "EventScripts"));
			}

			var diagnosticsDirectory = Path.Combine(instancePath, "Diagnostics");
			var minidumpsDirectory = Path.Combine(diagnosticsDirectory, "Minidumps");
			if (Directory.Exists(minidumpsDirectory))
			{
				Console.WriteLine("Renaming Minidumps folder to ProcessDumps...");
				Directory.Move(minidumpsDirectory, Path.Combine(diagnosticsDirectory, "ProcessDumps"));
			}

			Console.WriteLine("Deleting BYOND folder...");
			await RecursivelyDeleteDirectory(new DirectoryInfo(byondDirectory));
			Console.WriteLine("Deleting RepoKey folder...");
			await RecursivelyDeleteDirectory(new DirectoryInfo(Path.Combine(instancePath, "RepoKey")));
			Console.WriteLine("Deleting Game folder...");
			await RecursivelyDeleteDirectory(new DirectoryInfo(Path.Combine(instancePath, "Game")));
			Console.WriteLine("Deleting Instance.json...");
			File.Delete(Path.Combine(instancePath, "Instance.json"));
			Console.WriteLine("Deleting prtestjob.json...");
			File.Delete(Path.Combine(instancePath, "prtestjob.json"));
			Console.WriteLine("Deleting TGS3.json...");
			File.Delete(Path.Combine(instancePath, "TGS3.json"));
			Console.WriteLine("Deleting TGDreamDaemonBridge.dll...");
			File.Delete(Path.Combine(instancePath, "TGDreamDaemonBridge.dll"));

			Console.WriteLine("Attaching TGS6 instance...");
			var TGS6Instance = await TGS6Client.Instances.CreateOrAttach(new InstanceCreateRequest
			{
				ConfigurationType = ConfigurationType.Disallowed,
				Name = instanceName,
				Path = instancePath,
			}, default);

			Console.WriteLine($"Onlining TGS6 instance ID {TGS6Instance.Id}...");
			TGS6Instance = await TGS6Client.Instances.Update(new InstanceUpdateRequest
			{
				Online = true,
				Id = TGS6Instance.Id
			}, default);

			var v5InstanceClient = TGS6Client.Instances.CreateClient(TGS6Instance);

			if (byondVersionRequest != null)
			{
				Console.WriteLine("Triggering BYOND install job...");
				await v5InstanceClient.Engine.SetActiveVersion(byondVersionRequest, null, default);
			}

			if (repositoryUpdateRequest != null)
			{
				Console.WriteLine("Updating repository settings...");
				await v5InstanceClient.Repository.Update(repositoryUpdateRequest, default);
			}

			Console.WriteLine("Updating deployment settings...");
			await v5InstanceClient.DreamMaker.Update(dreamMakerRequest, default);

			Console.WriteLine("Updating DreamDaemon settings...");
			await v5InstanceClient.DreamDaemon.Update(dreamDaemonRequest, default);

			foreach(var chatBotCreateRequest in chatBotCreateRequests)
			{
				Console.WriteLine($"Creating chat bot {chatBotCreateRequest.Name}...");
				await v5InstanceClient.ChatBots.Create(chatBotCreateRequest, default);
			}

			Console.WriteLine($"Instance {instanceName} (TGS6 ID: {TGS6Instance.Id}) successfully migrated!");
		}

		Console.WriteLine("All enabled V3 instances migrated into V5 and detached from V3!");

		return 0;
	}

	static async Task RecursivelyDeleteDirectory(DirectoryInfo dir)
	{
		var tasks = new List<Task>();

		if (!dir.Exists)
			return;

		// check if we are a symbolic link
		if (!dir.Attributes.HasFlag(FileAttributes.Directory) || dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
		{
			dir.Delete();
			return;
		}

		foreach (var subDir in dir.EnumerateDirectories())
			tasks.Add(RecursivelyDeleteDirectory(subDir));

		foreach (var file in dir.EnumerateFiles())
		{
			file.Attributes = FileAttributes.Normal;
			file.Delete();
		}

		await Task.WhenAll(tasks);
		dir.Delete(true);
	}
}
