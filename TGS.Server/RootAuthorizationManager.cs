using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using TGS.Interface.Components;

namespace TGS.Server
{
	/// <summary>
	/// A <see cref="ServiceAuthorizationManager"/> used to determine only if the caller is an admin
	/// </summary>
	sealed class RootAuthorizationManager : ServiceAuthorizationManager
	{
		string LastSeenUser;
		public static readonly IList<ServiceAuthorizationManager> InstanceAuthManagers = new List<ServiceAuthorizationManager>();
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			var contract = operationContext.EndpointDispatcher.ContractName;
			if (contract == typeof(ITGConnectivity).Name)   //always allow connectivity checks
				return true;
			
			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;

			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);
			if(!authSuccess && contract == typeof(ITGLanding).Name)
				return InstanceAuthManagers.Any(x => x.CheckAccess(operationContext));

			var user = windowsIdent.Name;
			if (LastSeenUser != user)
			{
				LastSeenUser = user;
				Server.Logger.WriteAccess(String.Format("Root access from: {0}", user), authSuccess, Server.LoggingID);
			}
			return authSuccess;
		}
	}
}
