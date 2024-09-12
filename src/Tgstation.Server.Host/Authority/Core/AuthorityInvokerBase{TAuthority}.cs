using System;
using System.Linq;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <inheritdoc />
	abstract class AuthorityInvokerBase<TAuthority> : IAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// The <see cref="IAuthority"/> being invoked.
		/// </summary>
		protected TAuthority Authority { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityInvokerBase{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The value of <see cref="Authority"/>.</param>
		public AuthorityInvokerBase(TAuthority authority)
		{
			Authority = authority ?? throw new ArgumentNullException(nameof(authority));
		}

		/// <inheritdoc />
		IQueryable<TResult> IAuthorityInvoker<TAuthority>.InvokeQueryable<TResult>(Func<TAuthority, IQueryable<TResult>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);
			return authorityInvoker(Authority);
		}

		/// <inheritdoc />
		IQueryable<TApiModel> IAuthorityInvoker<TAuthority>.InvokeTransformableQueryable<TResult, TApiModel, TTransformer>(Func<TAuthority, IQueryable<TResult>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);
			var expression = new TTransformer().Expression;
			return authorityInvoker(Authority)
				.Select(expression);
		}
	}
}
