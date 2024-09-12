using System;
using System.Linq;

using Tgstation.Server.Host.Models;

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
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="IQueryable{T}"/> <typeparamref name="TResult"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> <typeparamref name="TResult"/> returned.</returns>
		IQueryable<TResult> InvokeQueryable<TResult>(Func<TAuthority, IQueryable<TResult>> authorityInvoker);

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the transformed result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> returned by the <typeparamref name="TAuthority"/>.</typeparam>
		/// <typeparam name="TApiModel">The returned <see cref="Type"/>.</typeparam>
		/// <typeparam name="TTransformer">The <see cref="ITransformer{TInput, TOutput}"/> for converting <typeparamref name="TResult"/>s to <typeparamref name="TApiModel"/>s.</typeparam>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="IQueryable{T}"/> <typeparamref name="TResult"/>.</param>
		/// <returns>A <see cref="IQueryable{T}"/> <typeparamref name="TResult"/> returned.</returns>
		IQueryable<TApiModel> InvokeTransformableQueryable<TResult, TApiModel, TTransformer>(Func<TAuthority, IQueryable<TResult>> authorityInvoker)
			where TResult : IApiTransformable<TResult, TApiModel, TTransformer>
			where TApiModel : notnull
			where TTransformer : ITransformer<TResult, TApiModel>, new();
	}
}
