﻿using System;
using System.Threading.Tasks;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// Invokes <typeparamref name="TAuthority"/>s from GraphQL endpoints.
	/// </summary>
	/// <typeparam name="TAuthority">The <see cref="IAuthority"/> invoked.</typeparam>
	public interface IGraphQLAuthorityInvoker<TAuthority> : IAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method with no success result.
		/// </summary>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse"/>.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Invoke(Func<TAuthority, ValueTask<AuthorityResponse>> authorityInvoker);

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The resulting <see cref="Type"/> of the return value.</typeparam>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TApiModel"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<TApiModel?> InvokeAllowMissing<TResult, TApiModel>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : TApiModel
			where TApiModel : notnull;

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The resulting <see cref="Type"/> of the return value.</typeparam>
		/// <typeparam name="TTransformer">The <see cref="ITransformer{TInput, TOutput}"/> for converting <typeparamref name="TResult"/>s to <typeparamref name="TApiModel"/>s.</typeparam>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TApiModel"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<TApiModel?> InvokeTransformableAllowMissing<TResult, TApiModel, TTransformer>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : notnull, IApiTransformable<TResult, TApiModel, TTransformer>
			where TApiModel : notnull
			where TTransformer : ITransformer<TResult, TApiModel>, new();

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the non-nullable result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The resulting <see cref="Type"/> of the return value.</typeparam>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TApiModel"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<TApiModel> Invoke<TResult, TApiModel>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : TApiModel
			where TApiModel : notnull;

		/// <summary>
		/// Invoke a <typeparamref name="TAuthority"/> method and get the non-nullable result.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="AuthorityResponse{TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TApiModel">The resulting <see cref="Type"/> of the return value.</typeparam>
		/// <typeparam name="TTransformer">The <see cref="ITransformer{TInput, TOutput}"/> for converting <typeparamref name="TResult"/>s to <typeparamref name="TApiModel"/>s.</typeparam>
		/// <param name="authorityInvoker">The <typeparamref name="TAuthority"/> <see cref="Func{T, TResult}"/> returning a <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TApiModel"/> generated for the resulting <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<TApiModel> InvokeTransformable<TResult, TApiModel, TTransformer>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : notnull, IApiTransformable<TResult, TApiModel, TTransformer>
			where TApiModel : notnull
			where TTransformer : ITransformer<TResult, TApiModel>, new();
	}
}
