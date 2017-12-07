using System;
using System.Reflection;
using System.Security.Principal;

namespace TGS.Server.Security
{
	interface IAuthorizationManager
	{
		bool CheckAccess(WindowsIdentity identity, Type componentType, MethodInfo methodInfo);
	}
}
