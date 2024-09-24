using System;
using System.Reflection;

using HotChocolate.Authorization;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Inherits the roles of <see cref="TgsAuthorizeAttribute"/>s for GraphQL endpoints.
	/// </summary>
	/// <typeparam name="TAuthority">The <see cref="IAuthority"/> being wrapped.</typeparam>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public sealed class TgsGraphQLAuthorizeAttribute<TAuthority> : AuthorizeAttribute
		where TAuthority : IAuthority
	{
		/// <summary>
		/// The name of the method targeted.
		/// </summary>
		public string MethodName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute{TAuthority}"/> class.
		/// </summary>
		/// <param name="methodName">The <typeparamref name="TAuthority"/> method name to inherit roles from.</param>
		public TgsGraphQLAuthorizeAttribute(string methodName)
		{
			ArgumentNullException.ThrowIfNull(methodName);

			var authorityType = typeof(TAuthority);
			var authorityMethod = authorityType.GetMethod(methodName)
				?? throw new InvalidOperationException($"Could not find method {methodName} on {authorityType}!");
			var authorizeAttribute = authorityMethod.GetCustomAttribute<TgsAuthorizeAttribute>()
				?? throw new InvalidOperationException($"Could not find method {authorityType}.{methodName}() has no {nameof(TgsAuthorizeAttribute)}!");
			MethodName = methodName;
			Roles = authorizeAttribute.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries);
			Apply = ApplyPolicy.Validation;
		}
	}
}
