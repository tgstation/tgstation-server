using System;
using System.Linq.Expressions;

using GreenDonut.Data;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="QueryContext{TEntity}"/>.
	/// </summary>
	static class QueryContextExtensions
	{
		public static QueryContext<AuthorityResponse<TResult>> AuthorityResponseWrap<TResult>(this QueryContext<TResult> queryContext)
		{
			ArgumentNullException.ThrowIfNull(queryContext);

			Expression<Func<AuthorityResponse<TResult>, TResult>> responseToResult = response => response.Result!;

			var authorityParameter = Expression.Parameter(typeof(AuthorityResponse<TResult>), "authorityResponse");
			var resultFromAuthority = Expression.Invoke(
				responseToResult,
				authorityParameter);

			// we don't need to do much
			Expression<Func<AuthorityResponse<TResult>, AuthorityResponse<TResult>>>? selector = null;
			if (queryContext.Selector != null)
			{
				var innerInvoke = Expression.Invoke(
					queryContext.Selector,
					resultFromAuthority);

				var outerInvoke = Expression.Invoke(
					AuthorityResponse<TResult>.MappingExpression(),
					innerInvoke);

				selector = Expression.Lambda<Func<AuthorityResponse<TResult>, AuthorityResponse<TResult>>>(
					outerInvoke,
					authorityParameter);
			}

			Expression<Func<AuthorityResponse<TResult>, bool>>? predicate = null;
			if (queryContext.Predicate != null)
			{
				predicate = Expression.Lambda<Func<AuthorityResponse<TResult>, bool>>(
					Expression.Invoke(
						queryContext.Predicate,
						resultFromAuthority),
					authorityParameter);
			}

			if (queryContext.Sorting?.Operations.Length > 0)
			{
				throw new NotImplementedException();
			}

			return new QueryContext<AuthorityResponse<TResult>>(
				selector,
				predicate);
		}

		public static QueryContext<TResult> AuthorityResponseUnwrap<TResult>(this QueryContext<AuthorityResponse<TResult>> queryContext)
		{
			ArgumentNullException.ThrowIfNull(queryContext);

			// assuming we were wrapped with AuthorityResponseWrap. This is hacky but it works
			// UNLESS it was compiled down, in which case we are D-O-N-E-FUCKED
			Expression<Func<TResult, TResult>>? selector = null;
			if (queryContext.Selector != null)
			{
				selector = (Expression<Func<TResult, TResult>>?)((InvocationExpression)((InvocationExpression)queryContext.Selector.Body).Arguments[0]).Expression;
			}

			Expression<Func<TResult, bool>>? predicate = null;
			if (queryContext.Predicate != null)
			{
				predicate = (Expression<Func<TResult, bool>>?)((InvocationExpression)queryContext.Predicate.Body).Expression;
			}

			if (queryContext.Sorting?.Operations.Length > 0)
			{
				throw new NotImplementedException();
			}

			return new QueryContext<TResult>(
				selector,
				predicate);
		}
	}
}
