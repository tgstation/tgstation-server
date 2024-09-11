using System;
using System.Reflection;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Inherits the roles of <see cref="TgsAuthorizeAttribute"/>s for REST endpoints.
	/// </summary>
	/// <typeparam name="TAuthority">The <see cref="IAuthority"/> being wrapped.</typeparam>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public sealed class TgsRestAuthorizeAttribute<TAuthority> : AuthorizeAttribute
		where TAuthority : IAuthority
	{
		/// <summary>
		/// The name of the method targeted.
		/// </summary>
		public string MethodName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsRestAuthorizeAttribute{TAuthority}"/> class.
		/// </summary>
		/// <param name="methodName">The <typeparamref name="TAuthority"/> method name to inherit roles from.</param>
		public TgsRestAuthorizeAttribute(string methodName)
		{
			ArgumentNullException.ThrowIfNull(methodName);

			var authorityType = typeof(TAuthority);
			var authorityMethod = authorityType.GetMethod(methodName);
			if (authorityMethod == null)
				throw new InvalidOperationException($"Could not find method {methodName} on {authorityType}!");

			var authorizeAttribute = authorityMethod.GetCustomAttribute<TgsAuthorizeAttribute>();
			if (authorizeAttribute == null)
				throw new InvalidOperationException($"Could not find method {authorityType}.{methodName}() has no {nameof(TgsAuthorizeAttribute)}!");

			MethodName = methodName;
			Roles = authorizeAttribute.Roles;
		}
	}
}
