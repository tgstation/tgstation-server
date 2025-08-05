using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Invokes <typeparamref name="TAuthority"/>s.
	/// </summary>
	/// <typeparam name="TAuthority">The <see cref="IAuthority"/> invoked.</typeparam>
	public interface IAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the result.
		/// </summary>
		/// <typeparam name="TResult">The returned <see cref="Type"/>.</typeparam>
		/// <param name="authorityInvoker">The authority invocation returning a <see cref="IQueryable{T}"/> <typeparamref name="TResult"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IQueryable{T}"/> <typeparamref name="TResult"/> returned on success or <see langword="null"/> if the requirements weren't satisfied.</returns>
		ValueTask<IQueryable<TResult>?> InvokeQueryable<TResult>(Func<TAuthority, RequirementsGated<IQueryable<TResult>>> authorityInvoker);
	}
}
