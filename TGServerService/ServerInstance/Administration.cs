using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	//note this only works with MACHINE LOCAL groups and admins for now
	//if someone wants AD shit, code it yourself
	sealed partial class ServerInstance : ServiceAuthorizationManager, ITGAdministration
	{
		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the Windows group authorized to access the <see cref="ServerInstance"/>
		/// </summary>
		SecurityIdentifier TheDroidsWereLookingFor;
		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object authLock = new object();
		/// <summary>
		/// The <see cref="WindowsIdentity.Name"/> of the last <see cref="WindowsIdentity"/> to attempt to access the <see cref="ServerInstance"/>
		/// </summary>
		string LastSeenUser;

		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the account the <see cref="Service"/> is running as
		/// </summary>
		readonly SecurityIdentifier ServiceSID = WindowsIdentity.GetCurrent().User;

		/// <inheritdoc />
		public string GetCurrentAuthorizedGroup()
		{
			try
			{
				if (TheDroidsWereLookingFor == null)
					return "ADMIN";
				
				string res = null;
				try
				{
					res = GroupPrincipal.FindByIdentity(new PrincipalContext(ContextType.Machine), IdentityType.Sid, TheDroidsWereLookingFor.Value).Name;
				}
				catch { }
				return res ?? GroupPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.Sid, TheDroidsWereLookingFor.Value).Name;
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

		/// <summary>
		/// Set <see cref="TheDroidsWereLookingFor"/> based off either an <paramref name="search"/>ed name or a string <see cref="SecurityIdentifier"/> from the config
		/// </summary>
		/// <param name="search">The name of the group to search for</param>
		/// <param name="useDomain">Recursive parameter used to check for the group using <see cref="ContextType.Domain"/> instead of <see cref="ContextType.Machine"/></param>
		/// <returns>The name of the group allowed to access the <see cref="ServerInstance"/> if it could be found, <see langword="null"/> otherwise</returns>
		string FindTheDroidsWereLookingFor(string search = null, bool useDomain = false)
		{
			//find the group that is authorized to use the tools
			var pc = new PrincipalContext(useDomain ? ContextType.Domain : ContextType.Machine);
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
					return useDomain ? null : FindTheDroidsWereLookingFor(search, true);
			}
			TheDroidsWereLookingFor = gp.Sid;
			if (search != null)
			{
				config.AuthorizedGroupSID = TheDroidsWereLookingFor.Value;
				config.Save();
			}
			return gp.Name;
		}
		
		/// <summary>
		/// Called by WCF whenever a component call is made. Checks to see that the supplied user account has access to the requested component
		/// </summary>
		/// <param name="operationContext">Various parameters about the operation supplied by WCF</param>
		/// <returns><see langword="true"/> if the supplied user account may use the requested component, <see langword="false"/> otherwise</returns>
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			var contract = operationContext.EndpointDispatcher.ContractName;

			if (contract == typeof(ITGConnectivity).Name)   //always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;

			if (contract == typeof(ITGInterop).Name)
			{    
				//only allow the same user the service is running as to use interop, because that's what DD is running as, and don't spam the logs with it unless it fails
				var result = windowsIdent.User == ServiceSID;
				if(!result)
					Service.WriteAccess(windowsIdent.Name, false);
				return result;
			}

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
					Service.WriteAccess(user, authSuccess);
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
						if (updateStat != ByondStatus.Idle)
							return "BYOND busy!";
						if (!Monitor.TryEnter(CompilerLock))
							return "Compiler locked!";

						try
						{
							if (compilerCurrentStatus != CompilerStatus.Uninitialized && compilerCurrentStatus != CompilerStatus.Initialized)
								return "Compiler busy!";
							if (!Monitor.TryEnter(watchdogLock))
								return "Watchdog locked!";
							try
							{
								if (currentStatus != DreamDaemonStatus.Offline)
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
											Service.WriteWarning(String.Format("Server move from {0} to {1} partial success: {2}", Config.ServerDirectory, new_location, e.ToString()), EventID.ServerMovePartial);
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
									Service.WriteInfo(String.Format("Server moved from {0} to {1}", Config.ServerDirectory, new_location), EventID.ServerMoveComplete);
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
				Service.WriteError(String.Format("Server move from {0} to {1} failed: {2}", Config.ServerDirectory, new_location, e.ToString()), EventID.ServerMoveFailed);
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string RecreateStaticFolder()
		{
			if (!Monitor.TryEnter(RepoLock))
				return "Repo locked!";
			try
			{
				if (!Monitor.TryEnter(watchdogLock))
					return "Watchdog locked!";
				try
				{
					if (!Monitor.TryEnter(configLock))
						return "Static dir locked!";
					try
					{
						if (currentStatus != DreamDaemonStatus.Offline)
							return "Watchdog running!";
						BackupAndDeleteStaticDirectory();
						InitialConfigureRepository();
					}
					finally
					{
						Monitor.Exit(configLock);
					}
				}
				finally
				{
					Monitor.Exit(watchdogLock);
				}
			}
			catch(Exception e)
			{
				return e.ToString();
			}
			finally
			{
				Monitor.Exit(RepoLock);
			}
			return null;
		}
	}
}
