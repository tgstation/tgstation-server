using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Security;
using TGS.Server.ChatCommands;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server
{
	//handles talking between the world and us
	sealed partial class Instance : ITGInterop
	{

		object topicLock = new object();
		const int CommsKeyLen = 64;
		string serviceCommsKey; //regenerated every DD restart

		//range of supported api versions
		const int AllowedMajorAPIVersion = 2;
		Version GameAPIVersion;

		const string SPInstanceName = "server_instance";

		//See code/modules/server_tools/server_tools.dm for command switch
		const string SCHardReboot = "hard_reboot";  //requests that dreamdaemon restarts when the round ends
		const string SCGracefulShutdown = "graceful_shutdown";  //requests that dreamdaemon stops when the round ends
		const string SCWorldAnnounce = "world_announce";	//sends param 'message' to the world
		const string SCListCustomCommands = "list_custom_commands"; //Get a list of commands supported by the server
		const string SCAPICompat = "api_compat";    //Tells the server we understand each other
		const string SCPlayerCount = "client_count";    //Gets the number of connected client

		/// <summary>
		/// String returned when a command completes successfully with no output
		/// </summary>
		const string SRetSuccess = "SUCCESS";

		const string SRKillProcess = "killme";
		const string SRIRCBroadcast = "irc";
		const string SRIRCAdminChannelMessage = "send2irc";
		const string SRWorldReboot = "worldreboot";
		const string SRAPIVersion = "api_ver";

		const string CCPHelpText = "help_text";
		const string CCPAdminOnly = "admin_only";
		const string CCPRequiredParameters = "required_parameters";

		/// <summary>
		/// The file name of the .dll that contains the <see cref="ITGInterop"/> bridge class
		/// </summary>
		const string BridgeDLLName = "TGDreamDaemonBridge.dll";
		/// <summary>
		/// The namespace that contains the <see cref="ITGInterop"/> bridge class. Used for reflection
		/// </summary>
		const string DreamDaemonBridgeNamespace = "Bridge";
		/// <summary>
		/// The <see cref="ITGInterop"/> bridge class. Used for reflection
		/// </summary>
		const string DreamDaemonBridgeType = "DreamDaemonBridge";

		List<Command> ServerChatCommands;

		void LoadServerChatCommands()
		{
			if (DaemonStatus() != DreamDaemonStatus.Online)
				return;
			var json = SendCommand(SCListCustomCommands);
			if (String.IsNullOrWhiteSpace(json))
				return;
			List<Command> tmp = new List<Command>();
			try
			{
				foreach (var I in JsonConvert.DeserializeObject<IDictionary<string, object>>(json))
				{
					var innerDick = ((JObject)I.Value).ToObject<IDictionary<string, object>>();
					var helpText = (string)innerDick[CCPHelpText];
					var adminOnly = ((long)innerDick[CCPAdminOnly]) == 1;
					var requiredParams = (int)((long)innerDick[CCPRequiredParameters]);
					tmp.Add(new ServerChatCommand(I.Key, helpText, adminOnly, requiredParams));
				}
				ServerChatCommands = tmp;
			}
			catch { }
		}

		//raw command string sent here via world.ExportService
		void HandleCommand(string cmd)
		{
			var splits = new List<string>(cmd.Split(' '));
			cmd = splits[0];
			splits.RemoveAt(0);

			bool APIValid;
			lock (topicLock)
			{
				APIValid = CheckAPIVersionConstraints();
			}

			if (!APIValid && cmd != SRAPIVersion)
				return;	//SPEAK THE LANGUAGE!!!

			switch (cmd)
			{
				case SRIRCBroadcast:
					SendMessage("GAME: " + String.Join(" ", splits), MessageType.GameInfo);
					break;
				case SRKillProcess:
					KillMe();
					break;
				case SRIRCAdminChannelMessage:
					SendMessage("RELAY: " + String.Join(" ", splits), MessageType.AdminInfo);
					break;
				case SRWorldReboot:
					WriteInfo("World Rebooted", EventID.WorldReboot);
					WriteCurrentDDLog("World rebooted");
					ServerChatCommands = null;
					ChatConnectivityCheck();
					lock (CompilerLock)
					{
						if (UpdateStaged)
						{
							UpdateStaged = false;
							lock (topicLock)
							{
								GameAPIVersion = null;  //needs updating
							}
							WriteInfo("Staged update applied", EventID.ServerUpdateApplied);
						}
					}
					break;
				case SRAPIVersion:
					lock (topicLock)
					{
						try
						{
							GameAPIVersion = new Version(splits[0]);
							if (!CheckAPIVersionConstraints())
								throw new Exception();
						}
						catch
						{
							WriteWarning(String.Format("API version of the game ({0}) is incompatible with the current supported API versions (3.{2}.x.x). Interop disabled.", splits.Count > 1 ? splits[1] : "NULL", AllowedMajorAPIVersion), EventID.APIVersionMismatch);
							GameAPIVersion = null;
							break;
						}
					}
					//This needs to be done asyncronously otherwise DD won't be able to process it, because it's waiting for THIS THREAD to return
					ThreadPool.QueueUserWorkItem(_ => SendCommand(SCAPICompat));
					break;
			}
		}

		public string SendCommand(string cmd)
		{
			lock (watchdogLock)
			{
				if (currentStatus != DreamDaemonStatus.Online)
					return "Error: Server Offline!";
				return SendTopic(String.Format("serviceCommsKey={0};command={1}", serviceCommsKey, cmd), currentPort);
			}
		}

		public int PlayerCount()
		{
			try
			{
				return Convert.ToInt32(SendCommand(SCPlayerCount));
			}
			catch
			{
				return -1;
			}
		}

		//requires topiclock
		bool CheckAPIVersionConstraints()
		{
			//major will never change for all of TGS3
			//we treat minor as major, build as minor, and revision as patch
			return !(GameAPIVersion == null || GameAPIVersion.Minor != AllowedMajorAPIVersion);
		}

		//Fuckery to diddle byond with the right packet to accept our girth
		string SendTopic(string topicdata, ushort port)
		{
			//santize the escape characters in accordance with http://www.byond.com/docs/ref/info.html#/proc/params2list
			lock (topicLock) {
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

		//Every time we make a new DD process we generate a new comms key for security
		//It's in world.params['server_service']
		void GenCommsKey()
		{
			var charsToRemove = new string[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "-", "+", "=", "[", "{", "]", "}", ";", ":", "<", ">", "|", ".", "/", "?" };
			serviceCommsKey = String.Empty;
			do {
				var tmp = Membership.GeneratePassword(CommsKeyLen, 0);
				foreach (var c in charsToRemove)
					tmp = tmp.Replace(c, String.Empty);
				serviceCommsKey += tmp;
			} while (serviceCommsKey.Length < CommsKeyLen);
			serviceCommsKey = serviceCommsKey.Substring(0, CommsKeyLen);
			WriteInfo("Service Comms Key set to: " + serviceCommsKey, EventID.CommsKeySet);
		}

		/// <inheritdoc />
		public Task InteropMessage(string command)
		{
			return Task.Run(() =>
			{
				try
				{
					HandleCommand(command);
				}
				catch (Exception e)
				{
					WriteWarning(String.Format("Handle command for \"{0}\" failed: {1}", command, e.ToString()), EventID.InteropCallException);
				}
			});
		}
	}
}
