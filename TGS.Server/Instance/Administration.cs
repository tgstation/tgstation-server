﻿using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Interface.Components;
using TGS.Server.Security;

namespace TGS.Server
{
	sealed partial class Instance : IAuthorizationManager, ITGAdministration
	{
		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the Windows group authorized to access the <see cref="Instance"/>
		/// </summary>
		SecurityIdentifier TheDroidsWereLookingFor;
		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object authLock = new object();
		/// <summary>
		/// The <see cref="WindowsIdentity.Name"/> of the last <see cref="WindowsIdentity"/> to attempt to access the <see cref="Instance"/>
		/// </summary>
		string LastSeenUser;

		/// <summary>
		/// The <see cref="SecurityIdentifier"/> of the account the <see cref="Server"/> is running as
		/// </summary>
		readonly SecurityIdentifier ServiceSID = WindowsIdentity.GetCurrent().User;

		void InitAdministration()
		{
			lock(RootAuthorizationManager.InstanceAuthManagers)
				RootAuthorizationManager.InstanceAuthManagers.Add(this);
			FindTheDroidsWereLookingFor();
		}

		/// <inheritdoc />
		public Task<string> GetCurrentAuthorizedGroup()
		{
			return Task.Run(() =>
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
			});
		}

		/// <inheritdoc />
		public Task<string> SetAuthorizedGroup(string groupName)
		{
			return Task.Run(() =>
			{
				if (groupName == null)
				{
					TheDroidsWereLookingFor = null;
					Config.AuthorizedUserGroupSID = null;
					Config.Save();
					return "ADMIN";
				}
				return FindTheDroidsWereLookingFor(groupName);
			});
		}

		/// <summary>
		/// Set <see cref="TheDroidsWereLookingFor"/> based off either an <paramref name="search"/>ed name or a string <see cref="SecurityIdentifier"/> from the config
		/// </summary>
		/// <param name="search">The name of the group to search for</param>
		/// <param name="useDomain">Recursive parameter used to check for the group using <see cref="ContextType.Domain"/> instead of <see cref="ContextType.Machine"/></param>
		/// <returns>The name of the group allowed to access the <see cref="Instance"/> if it could be found, <see langword="null"/> otherwise</returns>
		string FindTheDroidsWereLookingFor(string search = null, bool useDomain = false)
		{
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
			{
				Config.AuthorizedUserGroupSID = TheDroidsWereLookingFor.Value;
				Config.Save();
			}
			return gp.Name;
		}

		/// <summary>
		/// Cleans up the <see cref="ITGAdministration"/> component
		/// </summary>
		void DisposeAdministration()
		{
			lock(RootAuthorizationManager.InstanceAuthManagers)
				RootAuthorizationManager.InstanceAuthManagers.Remove(this);
		}
		
		/// <summary>
		/// Called by WCF whenever a component call is made. Checks to see that the supplied user account has access to the requested component
		/// </summary>
		/// <param name="operationContext">Various parameters about the operation supplied by WCF</param>
		/// <returns><see langword="true"/> if the supplied user account may use the requested component, <see langword="false"/> otherwise</returns>
		public bool CheckAccess(WindowsIdentity identity, Type componentType, MethodInfo methodInfo)
		{
			if (componentType == typeof(ITGConnectivity))   //always allow connectivity checks
				return true;

			if (componentType == typeof(ITGInterop))
			{    
				//only allow the same user the service is running as to use interop, because that's what DD is running as, and don't spam the logs with it unless it fails
				var result = identity.User == ServiceSID;
				if(!result)
					WriteAccess(identity.Name, false);
				return result;
			}

			var wp = new WindowsPrincipal(identity);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			//if we're not an admin, check that we aren't trying to access the admin interface
			if (!authSuccess && componentType != typeof(ITGAdministration) && TheDroidsWereLookingFor != null)
				authSuccess = wp.IsInRole(new SecurityIdentifier(Config.AuthorizedUserGroupSID));

			lock (authLock)
			{
				var user = identity.Name;
				if (LastSeenUser != user)
				{
					LastSeenUser = user;
					WriteAccess(user, authSuccess);
				}
			}
			return authSuccess;
		}

		public Task<string> RecreateStaticFolder()
		{
			return Task.Run(() =>
			{
				lock (RepoLock)
				{
					if (RepoBusy)
						return "Repo locked!";
					RepoBusy = true;
				}
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
				catch (Exception e)
				{
					return e.ToString();
				}
				finally
				{
					lock (RepoLock)
						RepoBusy = false;
				}
				return null;
			});
		}
	}
}
