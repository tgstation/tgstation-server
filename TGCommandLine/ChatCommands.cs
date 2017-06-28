using System;
using System.Collections.Generic;
using TGServiceInterface;

namespace TGCommandLine
{
	class IRCCommand : RootCommand
	{
		public IRCCommand()
		{
			Keyword = "irc";
			Children = new Command[] { new IRCNickCommand(), new IRCAuthCommand(), new IRCDisableAuthCommand(), new IRCServerCommand(), new ChatJoinCommand(TGChatProvider.IRC), new ChatPartCommand(TGChatProvider.IRC), new ChatListAdminsCommand(TGChatProvider.IRC), new ChatReconnectCommand(TGChatProvider.IRC), new ChatAddminCommand(TGChatProvider.IRC), new ChatDeadminCommand(TGChatProvider.IRC), new ChatEnableCommand(TGChatProvider.IRC), new ChatDisableCommand(TGChatProvider.IRC), new ChatStatusCommand(TGChatProvider.IRC), new IRCAuthModeCommand(), new IRCAuthLevelCommand() };
		}
		protected override string GetHelpText()
		{
			return "Manages the IRC bot";
		}
	}
	class DiscordCommand : RootCommand
	{
		public DiscordCommand()
		{
			Keyword = "discord";
			Children = new Command[] { new DiscordSetTokenCommand(), new ChatJoinCommand(TGChatProvider.Discord), new ChatPartCommand(TGChatProvider.Discord), new ChatListAdminsCommand(TGChatProvider.Discord), new ChatReconnectCommand(TGChatProvider.Discord), new ChatAddminCommand(TGChatProvider.Discord), new ChatDeadminCommand(TGChatProvider.Discord), new ChatEnableCommand(TGChatProvider.Discord), new ChatDisableCommand(TGChatProvider.Discord), new ChatStatusCommand(TGChatProvider.Discord) , new DiscordAuthModeCommand() };
		}
		protected override string GetHelpText()
		{
			return "Manages the Discord bot";
		}
	}
	class IRCNickCommand : Command
	{
		public IRCNickCommand()
		{
			Keyword = "nick";
			RequiredParameters = 1;
		}
		
		protected override string GetArgumentString()
		{
			return "<name>";
		}
		protected override string GetHelpText()
		{
			return "Sets the IRC nickname";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var Chat = Server.GetComponent<ITGChat>();
			Chat.SetProviderInfo(new TGIRCSetupInfo(Chat.ProviderInfos()[(int)TGChatProvider.IRC])
			{
				Nickname = parameters[0],
			});
			return ExitCode.Normal;
		}
	}

	class ChatJoinCommand : Command
	{
		readonly int providerIndex;
		public ChatJoinCommand(TGChatProvider pI)
		{
			Keyword = "join";
			RequiredParameters = 2;
			providerIndex = (int)pI;
		}

		protected override string GetArgumentString()
		{
			return "<channel> <dev|wd|game|admin>";
		}
		protected override string GetHelpText()
		{
			return "Joins a channel for listening and broadcasting of the specified message type (Developer, Watchdog, Game, Admin)";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[providerIndex];
			IList<string> channels;

			switch (parameters[1].ToLower())
			{
				case "dev":
					channels = info.DevChannels;
					break;
				case "wd":
					channels = info.WatchdogChannels;
					break;
				case "game":
					channels = info.GameChannels;
					break;
				case "admin":
					channels = info.AdminChannels;
					break;
				default:
					Console.WriteLine("Invalid parameter: " + parameters[1]);
					return ExitCode.BadCommand;
			}

			var lowerParam = parameters[0].ToLower();
			foreach (var I in channels)
			{
				if (I.ToLower() == lowerParam)
				{
					Console.WriteLine("Already in this channel!");
					return ExitCode.BadCommand;
				}
			}
			channels.Add(parameters[0]);
			switch (parameters[1].ToLower())
			{
				case "dev":
					info.DevChannels = channels;
					break;
				case "wd":
					info.WatchdogChannels = channels;
					break;
				case "game":
					info.GameChannels = channels;
					break;
				case "admin":
					info.AdminChannels = channels;
					break;
			}
			var res = IRC.SetProviderInfo(info);
			if(res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	class ChatPartCommand : Command
	{
		readonly int providerIndex;
		public ChatPartCommand(TGChatProvider pI)
		{
			Keyword = "part";
			RequiredParameters = 2;
			providerIndex = (int)pI;
		}
		
		protected override string GetArgumentString()
		{
			return "<channel> <dev|wd|game|admin>";
		}
		protected override string GetHelpText()
		{
			return "Stops listening and broadcasting on a channel for the specified message type (Developer, Watchdog, Game, Admin)";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[providerIndex];
			IList<string> channels;

			switch (parameters[1].ToLower())
			{
				case "dev":
					channels = info.DevChannels;
					break;
				case "wd":
					channels = info.WatchdogChannels;
					break;
				case "game":
					channels = info.GameChannels;
					break;
				case "admin":
					channels = info.AdminChannels;
					break;
				default:
					Console.WriteLine("Invalid parameter: " + parameters[1]);
					return ExitCode.BadCommand;
			}
			var lowerParam = parameters[0].ToLower();
			if ((TGChatProvider)providerIndex == TGChatProvider.IRC && lowerParam[0] != '#')
				lowerParam = "#" + lowerParam;
			channels.Remove(lowerParam);
			switch (parameters[1].ToLower())
			{
				case "dev":
					info.DevChannels = channels;
					break;
				case "wd":
					info.WatchdogChannels = channels;
					break;
				case "game":
					info.GameChannels = channels;
					break;
				case "admin":
					info.AdminChannels = channels;
					break;
			}
			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class ChatListAdminsCommand : Command
	{
		readonly int providerIndex;
		public ChatListAdminsCommand(TGChatProvider pI)
		{
			Keyword = "list-admins";
			providerIndex = (int)pI;
		}
		
		protected override string GetHelpText()
		{
			return "List users which can use restricted commands in the admin channel";
		}
		
		public override ExitCode Run(IList<string> parameters)
		{
			var info = Server.GetComponent<ITGChat>().ProviderInfos()[providerIndex];
			string authType;
			switch ((TGChatProvider)providerIndex)
			{
				case TGChatProvider.IRC:
					if (info.AdminsAreSpecial)
						authType = "Mode:";
					else
						authType = "Nicknames:";
					break;
				case TGChatProvider.Discord:
					if (info.AdminsAreSpecial)
						authType = "Role IDs:";
					else
						authType = "User IDs:";
					break;
				default:
					Console.WriteLine(String.Format("Invalid provider: {0}!", providerIndex));
					return ExitCode.ServerError;
			}
			Console.WriteLine("Authorized " + authType);
			if (info.AdminsAreSpecial && (TGChatProvider)providerIndex == TGChatProvider.IRC)
				switch(new TGIRCSetupInfo(info).AuthLevel)
				{
					case IRCMode.Voice:
						Console.WriteLine("+");
						break;
					case IRCMode.Halfop:
						Console.WriteLine("%");
						break;
					case IRCMode.Op:
						Console.WriteLine("@");
						break;
					case IRCMode.Owner:
						Console.WriteLine("~");
						break;
				}
			else
				foreach (var I in info.AdminList)
					Console.WriteLine(I);
			return ExitCode.Normal;
		}
	}
	class ChatReconnectCommand : Command
	{
		readonly TGChatProvider providerIndex;
		public ChatReconnectCommand(TGChatProvider pI)
		{
			Keyword = "reconnect";
			providerIndex = pI;
		}
		
		protected override string GetHelpText()
		{
			return "Restablish the chat connection";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var res = Server.GetComponent<ITGChat>().Reconnect(providerIndex);
			if (res != null)
			{
				Console.WriteLine("Error: " + res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class ChatAddminCommand : Command
	{
		readonly int providerIndex;
		public ChatAddminCommand(TGChatProvider pI)
		{
			Keyword = "addmin";
			RequiredParameters = 1;
			providerIndex = (int)pI;
		}

		protected override string GetArgumentString()
		{
			return "[nick]";
		}
		protected override string GetHelpText()
		{
			return "Add a user which can use restricted commands in the admin channels";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[providerIndex];
			var newmin = parameters[0].ToLower();

			if (info.AdminsAreSpecial && (TGChatProvider)providerIndex == TGChatProvider.IRC)
			{
				Console.WriteLine("Invalid auth mode for this command!");
				return ExitCode.BadCommand;
			}

			if (info.AdminList.Contains(newmin))
			{
				Console.WriteLine(parameters[0] + " is already an admin!");
				return ExitCode.BadCommand;
			}
			var al = info.AdminList;
			al.Add(newmin);
			info.AdminList = al;
			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class IRCAuthModeCommand : Command
	{
		public IRCAuthModeCommand()
		{
			Keyword = "set-auth-mode";
			RequiredParameters = 1;
		}

		protected override string GetArgumentString()
		{
			return "<channel-mode|nickname>";
		}
		protected override string GetHelpText()
		{
			return "Switch between admin command authorization via user channel mode or nicknames";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[(int)TGChatProvider.IRC];
			var lowerparam = parameters[0].ToLower();
			if (lowerparam == "channel-mode")
				info.AdminsAreSpecial = true;
			else if (lowerparam == "nickname")
				info.AdminsAreSpecial = false;
			else
			{
				Console.WriteLine("Invalid parameter: " + parameters[0]);
				return ExitCode.BadCommand;
			}
			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class DiscordAuthModeCommand : Command
	{
		public DiscordAuthModeCommand()
		{
			Keyword = "set-auth-mode";
			RequiredParameters = 1;
		}

		protected override string GetArgumentString()
		{
			return "<role-id|user-id>";
		}
		protected override string GetHelpText()
		{
			return "Switch between admin command authorization via user roles or individual users";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[(int)TGChatProvider.Discord];
			var lowerparam = parameters[0].ToLower();
			if (lowerparam == "role-id")
				info.AdminsAreSpecial = true;
			else if (lowerparam == "user-id")
				info.AdminsAreSpecial = false;
			else
			{
				Console.WriteLine("Invalid parameter: " + parameters[0]);
				return ExitCode.BadCommand;
			}
			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class IRCAuthLevelCommand : Command
	{
		public IRCAuthLevelCommand()
		{
			Keyword = "set-auth-level";
			RequiredParameters = 1;
		}

		protected override string GetArgumentString()
		{
			return "<+|%|@|~>";
		}
		protected override string GetHelpText()
		{
			return "Set the required channel mode for users to use admin commands";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = new TGIRCSetupInfo(IRC.ProviderInfos()[(int)TGChatProvider.IRC]);
			switch (parameters[0])
			{
				case "+":
					info.AuthLevel = IRCMode.Voice;
					break;
				case "%":
					info.AuthLevel = IRCMode.Halfop;
					break;
				case "@":
					info.AuthLevel = IRCMode.Op;
					break;
				case "~":
					info.AuthLevel = IRCMode.Owner;
					break;
				default:
					Console.WriteLine("Invalid parameter: " + parameters[0]);
					return ExitCode.BadCommand;
			}

			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class ChatDeadminCommand : Command
	{
		readonly int providerIndex;
		public ChatDeadminCommand(TGChatProvider pI)
		{
			Keyword = "deadmin";
			RequiredParameters = 1;
			providerIndex = (int)pI;
		}
		protected override string GetArgumentString()
		{
			return "<nick>";
		}
		protected override string GetHelpText()
		{
			return "Remove a user which can use restricted commands in the admin channels";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[providerIndex];
			var newmin = parameters[0].ToLower();
			
			if (info.AdminsAreSpecial && (TGChatProvider)providerIndex == TGChatProvider.IRC)
			{
				Console.WriteLine("Invalid auth mode for this command!");
				return ExitCode.BadCommand;
			}
			
			if (!info.AdminList.Contains(newmin))
			{
				Console.WriteLine(parameters[0] + " is not an admin!");
				return ExitCode.BadCommand;
			}

			var al = info.AdminList;
			al.Remove(newmin);
			info.AdminList = al;
			var res = IRC.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	class IRCAuthCommand : Command
	{
		public IRCAuthCommand()
		{
			Keyword = "setup-auth";
			RequiredParameters = 2;
		}

		protected override string GetArgumentString()
		{
			return "<target> <message>";
		}
		protected override string GetHelpText()
		{
			return "Set the authentication message to send to target for identification. e.g. NickServ \"identify hunter2\"";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			IRC.SetProviderInfo(new TGIRCSetupInfo(IRC.ProviderInfos()[(int)TGChatProvider.IRC])
			{
				AuthTarget = parameters[0],
				AuthMessage = parameters[1]
			});
			return ExitCode.Normal;
		}
	}

	class IRCDisableAuthCommand : Command
	{
		public IRCDisableAuthCommand()
		{
			Keyword = "disable-auth";
		}		
		protected override string GetHelpText()
		{
			return "Turns off IRC authentication";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			IRC.SetProviderInfo(new TGIRCSetupInfo(IRC.ProviderInfos()[(int)TGChatProvider.IRC])
			{
				AuthTarget = null,
				AuthMessage = null,
			});
			return ExitCode.Normal;
		}
	}

	class ChatStatusCommand : Command
	{
		readonly int providerIndex;
		public ChatStatusCommand(TGChatProvider pI)
		{
			Keyword = "status";
			providerIndex = (int)pI;
		}
		protected override string GetHelpText()
		{
			return "Lists channels and connections status";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var IRC = Server.GetComponent<ITGChat>();
			var info = IRC.ProviderInfos()[providerIndex];
			Console.WriteLine("Currently configured channels:");
			Console.WriteLine("Admin:");
			foreach (var I in info.AdminChannels)
				Console.WriteLine("\t" + I);
			Console.WriteLine("Watchdog:");
			foreach (var I in info.WatchdogChannels)
				Console.WriteLine("\t" + I);
			Console.WriteLine("Game:");
			foreach (var I in info.GameChannels)
				Console.WriteLine("\t" + I);
			Console.WriteLine("Developer:");
			foreach (var I in info.DevChannels)
				Console.WriteLine("\t" + I);
			Console.WriteLine("Chat bot is: " + (!info.Enabled ? "Disabled" : IRC.Connected(info.Provider) ? "Connected" : "Disconnected"));
			return ExitCode.Normal;
		}
	}
	class ChatEnableCommand : Command
	{
		readonly int providerIndex;
		public ChatEnableCommand(TGChatProvider pI)
		{
			Keyword = "enable";
			providerIndex = (int)pI;
		}

		protected override string GetHelpText()
		{
			return "Enables the chat bot";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var Chat = Server.GetComponent<ITGChat>();
			var info = Chat.ProviderInfos()[providerIndex];
			info.Enabled = true;
			var res = Chat.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}
	class ChatDisableCommand : Command
	{
		readonly int providerIndex;
		public ChatDisableCommand(TGChatProvider pI)
		{
			Keyword = "disable";
			providerIndex = (int)pI;
		}

		protected override string GetHelpText()
		{
			return "Disables the chat bot";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var Chat = Server.GetComponent<ITGChat>();
			var info = Chat.ProviderInfos()[providerIndex];
			info.Enabled = false;
			var res = Chat.SetProviderInfo(info);
			if (res != null)
			{
				Console.WriteLine(res);
				return ExitCode.ServerError;
			}
			return ExitCode.Normal;
		}
	}

	class IRCServerCommand : Command
	{
		public IRCServerCommand()
		{
			Keyword = "set-server";
			RequiredParameters = 1;
		}
		protected override string GetArgumentString()
		{
			return "<url>:<port>";
		}
		protected override string GetHelpText()
		{
			return "Sets the IRC server";
		}

		public override ExitCode Run(IList<string> parameters)
		{
			var splits = parameters[0].Split(':');
			if(splits.Length < 2)
			{
				Console.WriteLine("Invalid parameter!");
				return ExitCode.BadCommand;
			}
			var Chat = Server.GetComponent<ITGChat>();
			var PI = new TGIRCSetupInfo(Chat.ProviderInfos()[(int)TGChatProvider.IRC])
			{
				URL = splits[0]
			};
			try
			{
				PI.Port = Convert.ToUInt16(splits[1]);
			}
			catch
			{
				Console.WriteLine("Invalid port number!");
				return ExitCode.BadCommand;
			}

			var res = Chat.SetProviderInfo(PI);
			Console.WriteLine(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}

	class DiscordSetTokenCommand : Command
	{
		public DiscordSetTokenCommand()
		{
			Keyword = "set-token";
			RequiredParameters = 1;
		}
		protected override string GetArgumentString()
		{
			return "<bot-token>";
		}
		protected override string GetHelpText()
		{
			return "Sets the discord API bot token";
		}
		public override ExitCode Run(IList<string> parameters)
		{
			var Chat = Server.GetComponent<ITGChat>();
			var res = Chat.SetProviderInfo(new TGDiscordSetupInfo(Chat.ProviderInfos()[(int)TGChatProvider.Discord]) { BotToken = parameters[0] });
			Console.WriteLine(res ?? "Success");
			return res == null ? ExitCode.Normal : ExitCode.ServerError;
		}
	}
}