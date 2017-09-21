using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	//note this only works with MACHINE LOCAL groups and admins for now
	//if someone wants AD shit, code it yourself
	partial class TGStationServer : ServiceAuthorizationManager, ITGAdministration
	{
		SecurityIdentifier TheDroidsWereLookingFor;
		object authLock = new object();
		string LastSeenUser = null;

		readonly SecurityIdentifier ServiceSID = WindowsIdentity.GetCurrent().User;

		/// <inheritdoc />
		public string GetCurrentAuthorizedGroup()
		{
			try
			{
				if (TheDroidsWereLookingFor == null)
					return "ADMIN";

				var pc = new PrincipalContext(ContextType.Machine);
				return GroupPrincipal.FindByIdentity(pc, IdentityType.Sid, TheDroidsWereLookingFor.Value).Name;
			}
			catch
			{
				return null;
			}
		}

		/// <inheritdoc />
		public string SetAuthorizedGroup(string groupName)
		{
			if (groupName == null)
			{
				TheDroidsWereLookingFor = null;
				var config = Properties.Settings.Default;
				config.AuthorizedGroupSID = null;
				config.Save();
				return "ADMIN";
			}
			return FindTheDroidsWereLookingFor(groupName);
		}

		string FindTheDroidsWereLookingFor(string search = null)
		{
			//find the group that is authorized to use the tools
			var pc = new PrincipalContext(ContextType.Machine);
			var config = Properties.Settings.Default;
			var groupName = search ?? config.AuthorizedGroupSID;
			if (String.IsNullOrWhiteSpace(groupName))
				return null;
			var gp = GroupPrincipal.FindByIdentity(pc, search != null ? IdentityType.Name : IdentityType.Sid, groupName);
			if (gp == null)
			{
				if (search != null)
					//try again with all types
					gp = GroupPrincipal.FindByIdentity(pc, search);
				if (gp == null)
					return null;
			}
			TheDroidsWereLookingFor = gp.Sid;
			if (search != null)
			{
				config.AuthorizedGroupSID = TheDroidsWereLookingFor.Value;
				config.Save();
			}
			return gp.Name;
		}

		//This function checks for authorization whenever an API call is made
		//This does NOT validate the windows account, that is done when the user connects internally
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			var contract = operationContext.EndpointDispatcher.ContractName;

			if (contract == typeof(ITGConnectivity).Name)   //always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;

			if (contract == typeof(ITGInterop).Name)	//only allow the same user the service is running as to use interop, because that's what DD is running as
				return windowsIdent.User == ServiceSID;

			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			//if we're not an admin, check that we aren't trying to access the admin interface
			if (!authSuccess && operationContext.EndpointDispatcher.ContractName != typeof(ITGAdministration).Name && TheDroidsWereLookingFor != null)
				authSuccess = wp.IsInRole(new SecurityIdentifier(Properties.Settings.Default.AuthorizedGroupSID));

			lock (authLock)
			{
				var user = windowsIdent.Name;
				if (LastSeenUser != user)
				{
					LastSeenUser = user;
					TGServerService.WriteAccess(user, authSuccess);
				}
			}
			return authSuccess;
		}

		/// <inheritdoc />
		public ushort RemoteAccessPort()
		{
			return Properties.Settings.Default.RemoteAccessPort;
		}

		/// <inheritdoc />
		public string SetRemoteAccessPort(ushort port)
		{
			if (port == 0)
				return "Cannot bind to port 0";
			var Config = Properties.Settings.Default;
			Config.RemoteAccessPort = port;
			Config.Save();
			return null;
		}

		/// <inheritdoc />
		public string MoveServer(string new_location)
		{
			var Config = Properties.Settings.Default;
			try
			{
				var di1 = new DirectoryInfo(Config.ServerDirectory);
				var di2 = new DirectoryInfo(new_location);

				var copy = di1.Root.FullName != di2.Root.FullName;

				if (copy && File.Exists(PrivateKeyPath))
					return String.Format("Unable to perform a cross drive server move with the {0}. Copy aborted!", PrivateKeyPath);

				new_location = di2.FullName;

				while (di2.Parent != null)
					if (di2.Parent.FullName == di1.FullName)
						return "Cannot move to child of current directory!";
					else
						di2 = di2.Parent;

				if (!Monitor.TryEnter(RepoLock))
					return "Repo locked!";
				try
				{
					if (RepoBusy)
						return "Repo busy!";
					DisposeRepo();
					if (!Monitor.TryEnter(ByondLock))
						return "BYOND locked";
					try
					{
						if (updateStat != TGByondStatus.Idle)
							return "BYOND busy!";
						if (!Monitor.TryEnter(CompilerLock))
							return "Compiler locked!";

						try
						{
							if (compilerCurrentStatus != TGCompilerStatus.Uninitialized && compilerCurrentStatus != TGCompilerStatus.Initialized)
								return "Compiler busy!";
							if (!Monitor.TryEnter(watchdogLock))
								return "Watchdog locked!";
							try
							{
								if (currentStatus != TGDreamDaemonStatus.Offline)
									return "Watchdog running!";
								lock (configLock)
								{
									CleanGameFolder();
									Program.DeleteDirectory(GameDir);
									string error = null;
									if (copy)
									{
										Program.CopyDirectory(Config.ServerDirectory, new_location);
										Directory.CreateDirectory(new_location);
										Environment.CurrentDirectory = new_location;
										try
										{
											Program.DeleteDirectory(Config.ServerDirectory);
										}
										catch (Exception e)
										{
											error = "The move was successful, but the path " + Config.ServerDirectory + " was unable to be deleted fully!";
											TGServerService.WriteWarning(String.Format("Server move from {0} to {1} partial success: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMovePartial);
										}
									}
									else
									{
										try
										{
											Environment.CurrentDirectory = di2.Root.FullName;
											Directory.Move(Config.ServerDirectory, new_location);
											Environment.CurrentDirectory = new_location;
										}
										catch
										{
											Environment.CurrentDirectory = Config.ServerDirectory;
											throw;
										}
									}
									TGServerService.WriteInfo(String.Format("Server moved from {0} to {1}", Config.ServerDirectory, new_location), TGServerService.EventID.ServerMoveComplete);
									Config.ServerDirectory = new_location;
									return null;
								}
							}
							finally
							{
								Monitor.Exit(watchdogLock);
							}
						}
						finally
						{
							Monitor.Exit(CompilerLock);
						}
					}
					finally
					{
						Monitor.Exit(ByondLock);
					}
				}
				finally
				{
					Monitor.Exit(RepoLock);
				}
			}
			catch (Exception e)
			{
				TGServerService.WriteError(String.Format("Server move from {0} to {1} failed: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMoveFailed);
				return e.ToString();
			}
		}
	}
}
