using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;

namespace TGS.Server.Security
{
	sealed class WindowsAuthorizationPolicy : IAuthorizationPolicy
	{
		readonly WindowsIdentity identity;
		public WindowsAuthorizationPolicy(IntPtr identityToken)
		{
			identity = new WindowsIdentity(identityToken);
		}
		public ClaimSet Issuer => throw new NotImplementedException();

		public string Id => throw new NotImplementedException();

		public bool Evaluate(EvaluationContext evaluationContext, ref object state)
		{
			evaluationContext.Properties["Identities"] = new List<IIdentity> { identity };
			return true;
		}
	}
}
