using System.Security.Principal;
using System.ServiceModel;
using TGServiceInterface;

namespace TGServerService
{
	class AdministrativeAuthorizationManager : ServiceAuthorizationManager
	{
		string LastSeenUser;
		protected override bool CheckAccessCore(OperationContext operationContext)
		{
			var contract = operationContext.EndpointDispatcher.ContractName;

			if (contract == typeof(ITGConnectivity).Name)   //always allow connectivity checks
				return true;

			var windowsIdent = operationContext.ServiceSecurityContext.WindowsIdentity;

			var wp = new WindowsPrincipal(windowsIdent);
			//first allow admins
			var authSuccess = wp.IsInRole(WindowsBuiltInRole.Administrator);

			var user = windowsIdent.Name;
			if (LastSeenUser != user)
			{
				LastSeenUser = user;
				TGServerService.WriteAccess(user, authSuccess);
			}
			return authSuccess;
		}
	}
}
