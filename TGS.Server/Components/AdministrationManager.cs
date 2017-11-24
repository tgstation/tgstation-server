using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.ServiceModel;
using TGS.Interface.Components;
using TGS.Server.Configuration;
using TGS.Server.Logging;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class AdministrationManager : ServiceAuthorizationManager, IAdministrationManager
	{
		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the account the <see cref="Server"/> is running as
		/// </summary>
		static readonly SecurityIdentifier ServerSID = WindowsIdentity.GetCurrent().User;

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="AdministrationManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="AdministrationManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IStaticManager"/> for the <see cref="AdministrationManager"/>
		/// </summary>
		readonly IStaticManager Static;

		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the Windows group authorized to access the <see cref="Instance"/>
		/// </summary>
		SecurityIdentifier TheDroidsWereLookingFor;

		/// <summary>
		/// The <see cref="WindowsIdentity.Name"/> of the last <see cref="WindowsIdentity"/> to attempt to access the <see cref="Instance"/>
		/// </summary>
		string LastSeenUser;

		/// <summary>
		/// Construct a <see cref="AdministrationManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="_static">The values of <see cref="Static"/></param>
		public AdministrationManager(IInstanceLogger logger, IInstanceConfig config, IStaticManager _static)
		{
			Logger = logger;
			Config = config;
			Static = _static;

			FindTheDroidsWereLookingFor();
		}

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
				Config.AuthorizedUserGroupSID = null;
				return "ADMIN";
			}
			return FindTheDroidsWereLookingFor(groupName);
		}

		/// <summary>
		/// Set <see cref="TheDroidsWereLookingFor"/> based off either an <paramref name="search"/>ed name or a string <see cref="SecurityIdentifier"/> from the config
		/// </summary>
		/// <param name="search">The name of the group to search for</param>
		/// <param name="useDomain">Recursive parameter used to check for the group using <see cref="ContextType.Domain"/> instead of <see cref="ContextType.Machine"/></param>
		/// <returns>The name of the group allowed to access the <see cref="Instance"/> if it could be found, <see langword="null"/> otherwise</returns>
		string FindTheDroidsWereLookingFor(string search = null, bool useDomain = false)
		{
			RootAuthorizationManager.InstanceAuthManagers.Add(this);
			//find the group that is authorized to use the tools
			var pc = new PrincipalContext(useDomain ? ContextType.Domain : ContextType.Machine);
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
					return useDomain ? null : FindTheDroidsWereLookingFor(search, true);
			}
			TheDroidsWereLookingFor = gp.Sid;
			if (search != null)
				Config.AuthorizedUserGroupSID = TheDroidsWereLookingFor.Value;
			return gp.Name;
		}

		/// <summary>
		/// Cleans up the <see cref="ITGAdministration"/> component
		/// </summary>
		void DisposeAdministration()
		{
			RootAuthorizationManager.InstanceAuthManagers.Remove(this);
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

			/*	//disabled temporarily until attribute based permissions go through
			if (contract == typeof(ITGInterop).Name)
			{    
				//only allow the same user the service is running as to use interop, because that's what DD is running as, and don't spam the logs with it unless it fails
				var result = windowsIdent.User == ServiceSID;
				if(!result)
					WriteAccess(windowsIdent.Name, false);
				return result;
			}*/

			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			//if we're not an admin, check that we aren't trying to access the admin interface
			if (!authSuccess && operationContext.EndpointDispatcher.ContractName != typeof(ITGAdministration).Name && TheDroidsWereLookingFor != null)
				authSuccess = wp.IsInRole(new SecurityIdentifier(Config.AuthorizedUserGroupSID));

			lock (this)
			{
				var user = windowsIdent.Name;
				if (LastSeenUser != user)
				{
					LastSeenUser = user;
					Logger.WriteAccess(user, authSuccess);
				}
			}
			return authSuccess;
		}

		/// <inheritdoc />
		public string RecreateStaticFolder()
		{
			try
			{
				Static.Recreate();
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
	}
}
