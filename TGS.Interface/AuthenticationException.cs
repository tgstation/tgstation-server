using System;
using System.Security.Principal;

namespace TGS.Interface
{
	/// <summary>
	/// Exception representing <see cref="Proxying.RequestState.Unauthenticated"/>
	/// </summary>
	public sealed class AuthenticationException : Exception
	{
		/// <summary>
		/// Construct an <see cref="AuthenticationException"/>
		/// </summary>
		/// <param name="username">The name of the user that failed to authenticate. Uses the executing user's name if <see langword="null"/></param>
		public AuthenticationException(string username) : base(String.Format("Unable to authenticate user {0} with provided password!", username ?? WindowsIdentity.GetCurrent().Name)) { }
	}
}
