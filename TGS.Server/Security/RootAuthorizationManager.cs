using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Reflection;
using System.ServiceModel;
using TGS.Interface.Components;

namespace TGS.Server.Security
{
	/// <summary>
	/// A <see cref="ServiceAuthorizationManager"/> used to determine only if the caller is an admin
	/// </summary>
	sealed class RootAuthorizationManager : IAuthorizationManager
	{
		string LastSeenUser;
		public static readonly IList<IAuthorizationManager> InstanceAuthManagers = new List<IAuthorizationManager>();
		public bool CheckAccess(WindowsIdentity identity, Type componentType, MethodInfo methodInfo)
		{
			if (componentType == typeof(ITGConnectivity))   //always allow connectivity checks
				return true;

			var wp = new WindowsPrincipal(identity);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);
			if(!authSuccess && componentType == typeof(ITGLanding))
				lock(InstanceAuthManagers)
					return InstanceAuthManagers.Any(x => x.CheckAccess(identity, componentType, methodInfo));

			var user = identity.Name;
			if (LastSeenUser != user)
			{
				LastSeenUser = user;
				Server.Logger.WriteAccess(String.Format("Root: {0}", user), authSuccess, Server.LoggingID);
			}
			return authSuccess;
		}
	}
}
