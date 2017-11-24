using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using TGS.Interface.Components;
using TGS.Server.Logging;

namespace TGS.Server
{
	/// <summary>
	/// A <see cref="ServiceAuthorizationManager"/> used to determine only if the caller is an admin
	/// </summary>
	sealed class RootAuthorizationManager : ServiceAuthorizationManager
	{
		public static IList<ServiceAuthorizationManager> InstanceAuthManagers { get; private set; } = new List<ServiceAuthorizationManager>();

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RootAuthorizationManager"/>
		/// </summary>
		readonly ILogger Logger;
		
		string LastSeenUser;

		/// <summary>
		/// Construct a <see cref="RootAuthorizationManager"/>
		/// </summary>
		/// <param name="logger">The value for <see cref="Logger"/></param>
		public RootAuthorizationManager(ILogger logger)
		{
			Logger = logger;
		}

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
				lock(InstanceAuthManagers)
					return InstanceAuthManagers.Any(x => x.CheckAccess(operationContext));

			var user = windowsIdent.Name;
			if (LastSeenUser != user)
			{
				LastSeenUser = user;
				Logger.WriteAccess(String.Format("Root access from: {0}", user), authSuccess, Server.LoggingID);
			}
			return authSuccess;
		}
	}
}
