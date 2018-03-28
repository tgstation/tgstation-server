using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;

namespace TGS.Server.Security
{
	/// <summary>
	/// Implements an <see cref="IAuthorizationPolicy"/> for <see cref="WindowsIdentity"/>
	/// </summary>
	sealed class WindowsAuthorizationPolicy : IAuthorizationPolicy
	{
		/// <summary>
		/// The <see cref="WindowsIdentity"/> to use
		/// </summary>
		readonly WindowsIdentity identity;

		/// <summary>
		/// Construct a <see cref="WindowsAuthorizationPolicy"/>
		/// </summary>
		/// <param name="ident">The value of <see cref="identity"/></param>
		public WindowsAuthorizationPolicy(WindowsIdentity ident)
		{
			identity = ident;
		}

		/// <summary>
		/// Unused implementation of <see cref="IAuthorizationPolicy"/>
		/// </summary>
		public ClaimSet Issuer => throw new NotImplementedException();

		/// <summary>
		/// Unused implementation of <see cref="IAuthorizationPolicy"/>
		/// </summary>
		public string Id => throw new NotImplementedException();

		/// <summary>
		/// Attaches <see cref="identity"/> to <paramref name="evaluationContext"/>'s properties
		/// </summary>
		/// <param name="evaluationContext">The <see cref="EvaluationContext"/></param>
		/// <param name="state">A state object</param>
		/// <returns><see langword="true"/></returns>
		public bool Evaluate(EvaluationContext evaluationContext, ref object state)
		{
			evaluationContext.Properties["Identities"] = new List<IIdentity> { identity };
			return true;
		}
	}
}
