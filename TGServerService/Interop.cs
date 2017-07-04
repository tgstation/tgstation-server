﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Security;
using TGServiceInterface;

namespace TGServerService
{
	//handles talking between the world and us
	partial class TGStationServer
	{
		object topicLock = new object();
		const int CommsKeyLen = 64;
		string serviceCommsKey; //regenerated every DD restart

		Thread NudgeThread;
		object NudgeLock = new object();

		//See code/modules/server_tools/server_tools.dm for command switch
		const string SCHardReboot = "hard_reboot";  //requests that dreamdaemon restarts when the round ends
		const string SCGracefulShutdown = "graceful_shutdown";  //requests that dreamdaemon stops when the round ends
		const string SCWorldAnnounce = "world_announce";	//sends param 'message' to the world
		const string SCIRCCheck = "irc_check";  //returns game stats
		const string SCIRCStatus = "irc_status";	//returns admin stats
		const string SCNameCheck = "namecheck"; //returns keywords lookup
		const string SCAdminPM = "adminmsg";	//pms a target ckey
		const string SCAdminWho = "adminwho";   //lists admins

		const string SRKillProcess = "killme";
		const string SRIRCBroadcast = "irc";
		const string SRIRCAdminChannelMessage = "send2irc";
		const string SRWorldReboot = "worldreboot";

		//raw command string sent here via world.ExportService
		void HandleCommand(string cmd)
		{
			var splits = new List<string>(cmd.Split(' '));
			cmd = splits[0];
			splits.RemoveAt(0);

			switch (cmd)
			{
				case SRIRCBroadcast:
					SendMessage("GAME: " + String.Join(" ", splits), ChatMessageType.GameInfo);
					break;
				case SRKillProcess:
					KillMe();
					break;
				case SRIRCAdminChannelMessage:
					SendMessage("RELAY: " + String.Join(" ", splits), ChatMessageType.AdminInfo);
					break;
				case SRWorldReboot:
<<<<<<< HEAD
					TGServerService.WriteInfo("World Rebooted", TGServerService.EventID.WorldReboot);
					lock (CompilerLock)
					{
						if (UpdateStaged)
						{
							UpdateStaged = false;
							TGServerService.WriteInfo("Staged update applied", TGServerService.EventID.ServerUpdateApplied);
						}
					}
=======
					TGServerService.WriteInfo("World Rebooted", TGServerService.EventID.WorldReboot, this);
>>>>>>> Instances
					break;
			}
		}
		
		string SendCommand(string cmd)
		{
			lock (watchdogLock)
			{
				if (currentStatus != TGDreamDaemonStatus.Online)
					return "Error: Server Offline!";
				return SendTopic(String.Format("serviceCommsKey={0};command={1}", serviceCommsKey, cmd), currentPort);
			}
		}

		bool WorldAnnounce(string message)
		{
			return SendCommand(SCWorldAnnounce + ";message=" + message) == "SUCCESS" ;
		}

		string SendPM(string targetCkey, string sender, string message)
		{
			return SendCommand(String.Format("{3};target={0};sender={1};message={2}", targetCkey, sender, message, SCAdminPM));
		}

		string NameCheck(string targetCkey, string sender)
		{
			return SendCommand(String.Format("{2};target={0};sender={1}", targetCkey, sender, SCNameCheck));
		}

		//Fuckery to diddle byond with the right packet to accept our girth
		string SendTopic(string topicdata, ushort port)
		{
			lock (topicLock) {
				using (var topicSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { SendTimeout = 5000, ReceiveTimeout = 5000 })
				{
					try
					{
						topicSender.Connect(IPAddress.Loopback, port);

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

						topicSender.Send(packet);

						string returnedString = "NULL";
						try
						{
							var returnedData = new byte[512];
							topicSender.Receive(returnedData);
							var raw_string = Encoding.ASCII.GetString(returnedData).TrimEnd(new char[] { (char)0 }).Trim();
							if (raw_string.Length > 6)
								returnedString = raw_string.Substring(5, raw_string.Length - 6).Trim();
						}
						catch
						{
							returnedString = "Topic recieve error!";
						}
						finally
						{
							topicSender.Shutdown(SocketShutdown.Both);
						}

<<<<<<< HEAD
						TGServerService.WriteInfo("Topic: \"" + topicdata + "\" Returned: " + returnedString, TGServerService.EventID.TopicSent);
=======
						TGServerService.WriteInfo("Topic: \"" + topicdata + "\" Returned: " + returnedString, TGServerService.EventID.TopicSent, this);
>>>>>>> Instances
						return returnedString;
					}
					catch
					{
<<<<<<< HEAD
						TGServerService.WriteWarning("Failed to send topic: " + topicdata, TGServerService.EventID.TopicFailed);
=======
						TGServerService.WriteWarning("Failed to send topic: " + topicdata, TGServerService.EventID.TopicFailed, this);
>>>>>>> Instances
						return "Topic delivery failed!";
					}
				}
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
<<<<<<< HEAD
			TGServerService.WriteInfo("Service Comms Key set to: " + serviceCommsKey, TGServerService.EventID.CommsKeySet);
=======
			TGServerService.WriteInfo("Service Comms Key set to: " + serviceCommsKey, TGServerService.EventID.CommsKeySet, this);
>>>>>>> Instances
		}

		//Start listening for nudges on the configured port
		void InitInterop()
		{
			lock (NudgeLock)
			{
				ShutdownInteropNoLock();
				NudgeThread = new Thread(new ThreadStart(NudgeHandler)) { IsBackground = true };
				NudgeThread.Start();
			}
		}

		void ShutdownInterop()
		{
			lock (NudgeLock)
			{
				ShutdownInteropNoLock();
			}
		}

		void ShutdownInteropNoLock()
		{
			if (NudgeThread != null)
			{
				NudgeThread.Abort();
				NudgeThread.Join();
			}
		}
		
		void NudgeHandler()
		{
			try
			{
				var np = InteropPort(out string error);
				if (error != null)
				{
<<<<<<< HEAD
					TGServerService.WriteError("Unable to start nudge handler: " + error, TGServerService.EventID.NudgeStartFail);
=======
					TGServerService.WriteError("Unable to start nudge handler: " + error, TGServerService.EventID.NudgeStartFail, this);
>>>>>>> Instances
					return;
				}

				using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(new IPEndPoint(IPAddress.Any, np));
					listener.Listen(5);

					// Start listening for connections.  
					using (var clientConnected = new ManualResetEvent(false))
					{
						while (true)
						{
							// Program is suspended while waiting for an incoming connection.  
							clientConnected.Reset();
							listener.BeginAccept(delegate (IAsyncResult asyncResult)
							{
								try
								{
									var handler = listener.EndAccept(asyncResult);

									var bytes = new byte[1024];
									int bytesRec = handler.Receive(bytes);
									// Show the data on the console.  
									HandleCommand(Encoding.ASCII.GetString(bytes, 0, bytesRec));

									handler.Shutdown(SocketShutdown.Both);
									handler.Close();
									clientConnected.Set();
								}
								catch (ObjectDisposedException)
								{ }
							}, null);
							clientConnected.WaitOne();
						}
					}
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
<<<<<<< HEAD
				TGServerService.WriteError("Nudge handler thread crashed: " + e.ToString(), TGServerService.EventID.NudgeCrash);
=======
				TGServerService.WriteError("Nudge handler thread crashed: " + e.ToString(), TGServerService.EventID.NudgeCrash, this);
>>>>>>> Instances
			}
		}
	}
}
