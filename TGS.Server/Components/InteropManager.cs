using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class InteropManager : IInteropManager
	{
		/// <summary>
		/// The filename of the dll that contains the bridge class
		/// </summary>
		public const string BridgeDLLName = "TGDreamDaemonBridge.dll";

		/// <summary>
		/// The namespace that contains the <see cref="DreamDaemonBridgeType"/>. Used for reflection
		/// </summary>
		const string DreamDaemonBridgeNamespace = "Bridge";
		/// <summary>
		/// The DreamDaemon bridge class. Used for reflection
		/// </summary>
		const string DreamDaemonBridgeType = "DreamDaemonBridge";
		/// <summary>
		/// The allowed major <see cref="DMAPIVersion"/>
		/// </summary>
		const int AllowedMajorAPIVersion = 2;
		/// <summary>
		/// How many characters <see cref="communicationsKey"/> should be
		/// </summary>
		const int CommsKeyLen = 64;

		// Interop API, make sure this stuff matches the DMAPI

		/// <summary>
		/// The command line parameter to specify <see cref="communicationsKey"/>
		/// </summary>
		const string CLParamCommunicationsKey = "server_service";
		/// <summary>
		/// The command line parameter to specify <see cref="Server.VersionString"/>
		/// </summary>
		const string CLParamServerVersion = "server_service_version";
		/// <summary>
		/// The command line parameter to specify <see cref="IInstanceConfig.Name"/>
		/// </summary>
		const string CLParamInstanceName = "server_instance";

		// Bridge commands
		/// <summary>
		/// Raises <see cref="OnKillRequest"/>
		/// </summary>
		const string BCKillProcess = "killme";
		/// <summary>
		/// Broadcasts the parameter using <see cref="MessageType.GameInfo"/>
		/// </summary>
		const string BCChatBroadcast = "irc";
		/// <summary>
		/// Broadcasts the parameter using <see cref="MessageType.AdminInfo"/>
		/// </summary>
		const string BCAdminChannelMessage = "send2irc";
		/// <summary>
		/// Raises <see cref="OnWorldReboot"/> among other things
		/// </summary>
		const string BCWorldReboot = "worldreboot";
		/// <summary>
		/// Sets <see cref="DMAPIVersion"/> to the parameter
		/// </summary>
		const string BCAPIVersion = "api_ver";

		// Topic commands
		/// <summary>
		/// <see cref="string"/> returned when a /world/Topic() command completes successfully with no output
		/// </summary>
		const string TCRetSuccess = "SUCCESS";
		/// <summary>
		/// Parameter key for sending the <see cref="communicationsKey"/> to /world/Topic()
		/// </summary>
		const string TCCommsKey = "serviceCommsKey";
		/// <summary>
		/// Parameter key for sending one of the <see cref="TopicCommands"/> to /world/Topic()
		/// </summary>
		const string TCCommand = "command";
		/// <summary>
		/// Map of <see cref="InteropCommand"/>s to /world/Topic() parameter <see cref="string"/>s
		/// </summary>
		static readonly IReadOnlyDictionary<InteropCommand, string> TopicCommands = new Dictionary<InteropCommand, string> {
			{ InteropCommand.RestartOnWorldReboot, "hard_reboot" },
			{ InteropCommand.ShutdownOnWorldReboot, "graceful_shutdown" },
			{ InteropCommand.WorldAnnounce, "world_announce" },
			{ InteropCommand.ListCustomCommands, "list_custom_commands" },
			{ InteropCommand.DMAPIIsCompatible, "api_compat" },
			{ InteropCommand.PlayerCount, "client_count" },
		};

		/// <inheritdoc />
		public event EventHandler OnKillRequest;
		/// <inheritdoc />
		public event EventHandler OnWorldReboot;

		/// <inheritdoc />
		public ushort TopicPort { private get; set; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InteropManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="InteropManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="InteropManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="InteropManager"/>
		/// </summary>
		readonly IChatManager Chat;

		/// <summary>
		/// The know DMAPI version the world is running
		/// </summary>
		Version DMAPIVersion;
		/// <summary>
		/// The communications key for authenticating /world/Topic() calls
		/// </summary>
		string communicationsKey;

		/// <summary>
		/// Properly escapes characters for a BYOND Topic() packet. See http://www.byond.com/docs/ref/info.html#/proc/list2params
		/// </summary>
		/// <param name="input">The <see cref="string"/> to sanitize</param>
		/// <returns>The sanitized string</returns>
		public static string SanitizeTopicString(string input)
		{
			return input.Replace("%", "%25").Replace("=", "%3d").Replace(";", "%3b").Replace("&", "%26").Replace("+", "%2b");
		}

		/// <summary>
		/// Construct an <see cref="InteropManager"/>
		/// </summary>
		/// <param name="io">The value of <see cref="IO"/></param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		public InteropManager(IIOManager io, IInstanceLogger logger, IInstanceConfig config, IChatManager chat)
		{
			IO = io;
			Logger = logger;
			Config = config;
			Chat = chat;

			TopicPort = Config.ReattachPort;
			OnWorldReboot += (a, b) => ResetDMAPIVersion();
			Chat.OnRequireChatCommands += (a, b) => Chat.LoadServerChatCommands(SendCommand(InteropCommand.ListCustomCommands));
		}

		/// <summary>
		/// Handle a <paramref name="command"/> from DMAPI's /world/proc/ExportService(command)
		/// </summary>
		/// <param name="command">The command to handle</param>
		void HandleBridgeCommand(string command)
		{
			var splits = new List<string>(command.Split(' '));
			command = splits[0];
			splits.RemoveAt(0);

			bool APIValid;
			lock (this)
				APIValid = CheckAPIVersionConstraints();

			if (!APIValid && command != BCAPIVersion)
				return; //SPEAK THE LANGUAGE!!!

			switch (command)
			{
				case BCChatBroadcast:
					Chat.SendMessage("GAME: " + String.Join(" ", splits), MessageType.GameInfo);
					break;
				case BCKillProcess:
					OnKillRequest(this, new EventArgs());
					break;
				case BCAdminChannelMessage:
					Chat.SendMessage("RELAY: " + String.Join(" ", splits), MessageType.AdminInfo);
					break;
				case BCWorldReboot:
					Logger.WriteInfo("World Rebooted", EventID.WorldReboot);
					Chat.ResetChatCommands();
					Chat.CheckConnectivity();
					OnWorldReboot(this, new EventArgs());
					break;
				case BCAPIVersion:
					lock (this)
						try
						{
							DMAPIVersion = new Version(splits[0]);
							if (!CheckAPIVersionConstraints())
								throw new Exception();
						}
						catch
						{
							Logger.WriteWarning(String.Format("API version of the game ({0}) is incompatible with the current supported API versions (3.{2}.x.x). Interop disabled.", splits.Count > 1 ? splits[1] : "NULL", AllowedMajorAPIVersion), EventID.APIVersionMismatch);
							ResetDMAPIVersion();
							break;
						}
					//This needs to be done asyncronously otherwise DD won't be able to process it, because it's waiting for THIS THREAD to return
					Task.Factory.StartNew(() => SendCommand(InteropCommand.DMAPIIsCompatible));
					break;
			}
		}

		/// <summary>
		/// Check that the world's <see cref="DMAPIVersion"/> is in accordance with <see cref="AllowedMajorAPIVersion"/>
		/// </summary>
		/// <returns><see langword="true"/> if we can interop with the world, <see langword="false"/> otherwise</returns>
		bool CheckAPIVersionConstraints()
		{
			lock (this)
				//major will never change for all of TGS3
				//we treat minor as major, build as minor, and revision as patch
				//see CONTRIBUTING.md for details
				return !(DMAPIVersion == null || DMAPIVersion.Minor != AllowedMajorAPIVersion);
		}

		/// <summary>
		/// Generate a random communications key that is <see cref="CommsKeyLen"/> long
		/// </summary>
		/// <returns>A random communications key</returns>
		string GenerateCommunicationsKey()
		{
			var charsToRemove = new string[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "-", "+", "=", "[", "{", "]", "}", ";", ":", "<", ">", "|", ".", "/", "?" };
			var res = String.Empty;
			do
			{
				var tmp = Membership.GeneratePassword(CommsKeyLen, 0);
				foreach (var c in charsToRemove)
					tmp = tmp.Replace(c, String.Empty);
				res += tmp;
			} while (res.Length < CommsKeyLen);
			res = res.Substring(0, CommsKeyLen);
			return res;
		}

		/// <summary>
		/// Call /world/Topic() with the given <paramref name="topicdata"/> on <see cref="TopicPort"/>. No-op if <see cref="DMAPIVersion"/> is <see langword="null"/>
		/// </summary>
		/// <param name="topicdata">The topic parameter list that can be read by http://www.byond.com/docs/ref/info.html#/proc/params2list</param>
		/// <returns>The return value of /world/Topic() on success, error message on failure</returns>
		string SendTopic(string topicdata)
		{
			var port = TopicPort;
			lock (this)
			{
				if (!CheckAPIVersionConstraints())
					return "Incompatible API!";

				StringBuilder stringPacket = new StringBuilder();
				stringPacket.Append((char)'\x00', 8);
				stringPacket.Append('?' + topicdata);
				stringPacket.Append((char)'\x00');
				string fullString = stringPacket.ToString();
				var packet = Encoding.ASCII.GetBytes(fullString);
				packet[1] = 0x83;
				var FinalLength = packet.Length - 4;
				if (FinalLength > UInt16.MaxValue)
					return "Error: Topic too long";

				var lengthBytes = BitConverter.GetBytes((ushort)FinalLength);

				packet[2] = lengthBytes[1]; //fucking endianess
				packet[3] = lengthBytes[0];

				var returnedString = "NULL";
				var returnedData = new byte[UInt16.MaxValue];
				using (var topicSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { SendTimeout = 5000, ReceiveTimeout = 5000 })
					try
					{
						topicSender.Connect(IPAddress.Loopback, port);
						topicSender.Send(packet);

						try
						{
							topicSender.Receive(returnedData);
							var raw_string = Encoding.ASCII.GetString(returnedData).TrimEnd(new char[] { (char)0 }).Trim();
							if (raw_string.Length > 6)
								returnedString = raw_string.Substring(5, raw_string.Length - 5).Trim();
						}
						catch
						{
							returnedString = "Topic recieve error!";
						}
						finally
						{
							topicSender.Shutdown(SocketShutdown.Both);
						}
					}
					catch
					{
						return "Topic delivery failed!";
					}

				return returnedString;
			}
		}

		/// <inheritdoc />
		public void UpdateBridgeDll(bool overwrite)
		{
			var FileExists = IO.FileExists(BridgeDLLName);
			if (FileExists && !overwrite)
				return;
			//Copy the interface dll to the static dir

			var FullInterfacePath = Assembly.GetAssembly(typeof(IServerInterface)).Location;
			//bridge is installed next to the interface
			var FullBridgePath = Path.Combine(Path.GetDirectoryName(FullInterfacePath), BridgeDLLName);
#if DEBUG
			//We could be debugging from the project directory
			if (!IO.FileExists(FullBridgePath))
				//A little hackish debug mode doctoring never hurt anyone
				FullBridgePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(InterfacePath)))), "TGS.Interface.Bridge/bin/x86/Debug", BridgeDLLName);
#endif
			try
			{
				//Use reflection to ensure these are the droids we're looking for
				Assembly.ReflectionOnlyLoadFrom(FullBridgePath).GetType(String.Format("{0}.{1}.{2}.{3}", nameof(TGS), nameof(Interface), DreamDaemonBridgeNamespace, DreamDaemonBridgeType), true);
			}
			catch (Exception e)
			{
				Logger.WriteError(String.Format("Unable to locate {0}! Error: {1}", BridgeDLLName, e.ToString()), EventID.BridgeDLLUpdateFail);
				return;
			}

			try
			{
				if (FileExists)
				{
					var Old = IO.ReadAllBytes(BridgeDLLName);
					var New = IO.ReadAllBytes(FullBridgePath);
					if (Old.Result.SequenceEqual(New.Result))
						return; //no need
				}
				IO.CopyFile(FullBridgePath, BridgeDLLName, overwrite).Wait();
			}
			catch
			{
				try
				{
					//ok the things being stupid and hasn't released the dll yet, try ONCE more
					Thread.Sleep(1000);
					IO.CopyFile(FullBridgePath, BridgeDLLName, overwrite).Wait();
				}
				catch (Exception e)
				{
					//intentionally using the fi
					Logger.WriteError("Failed to update bridge DLL! Error: " + e.ToString(), EventID.BridgeDLLUpdateFail);
					return;
				}
			}
			Logger.WriteInfo("Updated interface DLL", EventID.BridgeDLLUpdated);
		}

		/// <inheritdoc />
		public string StartParameters()
		{
			return String.Format("server_service={0}&server_service_version={1}&server_instance={2}",
				CLParamCommunicationsKey, communicationsKey,
				CLParamServerVersion, Server.VersionString,
				CLParamInstanceName, Config.Name);
		}

		/// <inheritdoc />
		public void SetCommunicationsKey(string newKey = null)
		{
			communicationsKey = newKey ?? GenerateCommunicationsKey();
			Logger.WriteInfo("Service Comms Key set to: " + communicationsKey, EventID.CommsKeySet);
		}

		/// <inheritdoc />
		public void WorldAnnounce(string message)
		{
			SendCommand(InteropCommand.WorldAnnounce, new List<string> { SanitizeTopicString(message) });
		}

		/// <inheritdoc />
		public string SendCommand(InteropCommand command, IEnumerable<string> parameters = null)
		{
			uint paramsRequired;
			switch (command)
			{
				case InteropCommand.CustomCommand:
				case InteropCommand.WorldAnnounce:
					paramsRequired = 1;
					break;
				default:
					paramsRequired = 0;
					break;
			}

			var paramCount = parameters != null ? parameters.Count() : 0;
			if (paramsRequired != paramCount)
				throw new InvalidOperationException(String.Format("Invalid number of parameters! Expected {0} got {1}", paramsRequired, paramCount));

			string commandText = String.Format("{0}={1}", TCCommsKey, communicationsKey);

			if (command != InteropCommand.CustomCommand)
				commandText = String.Format("{0};{2}={3}", commandText, TCCommand, TopicCommands[command]);

			switch (command)
			{
				case InteropCommand.WorldAnnounce:
					var message = parameters.First();
					commandText = String.Format("{0};message={1}", commandText, message);
					break;
				case InteropCommand.CustomCommand:
					var customCommand = parameters.First();
					commandText = String.Format("{0}{1}", commandText, customCommand);
					break;
			}

			return SendTopic(commandText);
		}

		/// <inheritdoc />
		public void ResetDMAPIVersion()
		{
			lock (this)
				DMAPIVersion = null;
		}

		/// <inheritdoc />
		public bool InteropMessage(string command)
		{
			try
			{
				HandleBridgeCommand(command);
				return true;
			}
			catch (Exception e)
			{
				Logger.WriteWarning(String.Format("Handle command for \"{0}\" failed: {1}", command, e.ToString()), EventID.InteropCallException);
				return false;
			}
		}

		/// <inheritdoc />
		public int PlayerCount()
		{
			try
			{
				return Convert.ToInt32(SendCommand(InteropCommand.PlayerCount));
			}
			catch
			{
				return -1;
			}
		}
	}
}
