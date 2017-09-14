using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using TGServiceInterface;

namespace TGServerService
{
	partial class TGStationServer : ServiceAuthorizationManager, ITGAdministration
	{
		SecurityIdentifier TheDroidsWereLookingFor;

		/// <inheritdoc />
		public string GetCurrentAuthorizedGroup()
		{
			try
			{
				return TheDroidsWereLookingFor != null ? TheDroidsWereLookingFor.Translate(typeof(GroupPrincipal)).ToString() : "ADMIN";
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
				config.AuthorizedGroupName = null;
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
			var groupName = search ?? config.AuthorizedGroupName;
			if (String.IsNullOrWhiteSpace(groupName))
				return null;
			var gp = GroupPrincipal.FindByIdentity(pc, search != null ? IdentityType.Name : IdentityType.Sid, groupName);
			if (gp == null)
				return null;
			TheDroidsWereLookingFor = gp.Sid;
			if (search != null)
			{
				config.AuthorizedGroupName = TheDroidsWereLookingFor.Value;
				config.Save();
			}
			return gp.Name;
		}
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			if (operationContext.EndpointDispatcher.ContractName == typeof(ITGConnectivity).Name)	//always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;
			//first allow admins
			var authSuccess = new WindowsPrincipal(windowsIdent).IsInRole(WindowsBuiltInRole.Administrator);

			//if we're not an admin, check that we aren't trying to access the admin interface
			if (!authSuccess && operationContext.EndpointDispatcher.ContractName == typeof(ITGAdministration).Name)
				//and allow those in the authorized group
				authSuccess = (TheDroidsWereLookingFor != null || windowsIdent.Groups.Contains(TheDroidsWereLookingFor));

			var actions = new List<string>();
			try
			{
				var realfilter = (ActionMessageFilter)operationContext.EndpointDispatcher.ContractFilter;
				var cnamespace = operationContext.EndpointDispatcher.ContractNamespace;
				foreach (var I in realfilter.Actions)
					actions.Add(I.Replace(cnamespace, "")); //filter out some garbage
			}
			catch (Exception e)
			{
				TGServerService.WriteError("IF YOU SEE THIS CALL CYBERBOSS: " + e.ToString(), TGServerService.EventID.Authentication);
			}
			TGServerService.WriteAccess(operationContext.ServiceSecurityContext.WindowsIdentity.Name, actions, authSuccess);
			return authSuccess;
		}
	}
}
