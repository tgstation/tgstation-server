using System;
using System.DirectoryServices.AccountManagement;
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
				Config.AuthorizedUserGroupSID = null;
				Config.Save();
				return "ADMIN";
			}
			return FindTheDroidsWereLookingFor(groupName);
		}

		string FindTheDroidsWereLookingFor(string search = null)
		{
			//find the group that is authorized to use the tools
			var pc = new PrincipalContext(ContextType.Machine);
			var groupName = search ?? Config.AuthorizedUserGroupSID;
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
				Config.AuthorizedUserGroupSID = TheDroidsWereLookingFor.Value;
				Config.Save();
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
				authSuccess = wp.IsInRole(new SecurityIdentifier(Config.AuthorizedUserGroupSID));

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
						if (currentStatus != TGDreamDaemonStatus.Offline)
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
