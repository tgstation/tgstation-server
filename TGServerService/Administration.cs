using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.ServiceModel;
using TGServiceInterface;

namespace TGServerService
{
	//note this only works with MACHINE LOCAL groups and admins for now
	//if someone wants AD shit, code it yourself
	partial class TGStationServer : ServiceAuthorizationManager, ITGAdministration
	{
		SecurityIdentifier TheDroidsWereLookingFor;

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
			if(groupName == null)
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
			if (operationContext.EndpointDispatcher.ContractName == typeof(ITGConnectivity).Name)	//always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;
			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			//if we're not an admin, check that we aren't trying to access the admin interface
			if (!authSuccess && operationContext.EndpointDispatcher.ContractName != typeof(ITGAdministration).Name && TheDroidsWereLookingFor != null)
			{
				var pc = new PrincipalContext(ContextType.Machine);
				var up = UserPrincipal.FindByIdentity(pc, IdentityType.Sid, windowsIdent.User.Value);
				//tiny bit of ad support here just cause i was debugging at work
				//if up is null check it on a domain
				if (up == null)
					try
					{
						up = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.Sid, windowsIdent.User.Value);
					}
					catch { }
				if (up != null)
				{
					var gp = GroupPrincipal.FindByIdentity(pc, IdentityType.Sid, TheDroidsWereLookingFor.Value);
					if (gp != null)
					{
						//and allow those in the authorized group
						authSuccess = up.IsMemberOf(gp);
					}
				}
			}
			
			TGServerService.WriteAccess(operationContext.ServiceSecurityContext.WindowsIdentity.Name, authSuccess);
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
	}
}
