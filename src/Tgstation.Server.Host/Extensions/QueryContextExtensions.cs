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
		/// <summary>
		/// Convert a <see cref="QueryContext{TEntity}"/> to an equivalent one that operates on a given <typeparamref name="TChild"/> of the original <typeparamref name="TParent"/>.
		/// </summary>
		/// <typeparam name="TParent">The parent <see cref="Type"/>.</typeparam>
		/// <typeparam name="TChild">The child <see cref="Type"/>.</typeparam>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> to transform.</param>
		/// <param name="fallback">A <typeparamref name="TChild"/> used in cas the <see cref="QueryContext{TEntity}.Selector"/> conversion does not yield a <typeparamref name="TChild"/>.</param>
		/// <returns>A new <see cref="QueryContext{TEntity}"/> for <typeparamref name="TChild"/> that is functionally identical to the original <paramref name="queryContext"/>.</returns>
		public static QueryContext<TChild> UpcastFrom<TParent, TChild>(this QueryContext<TParent> queryContext, Expression<Func<TChild>> fallback)
			where TChild : class, TParent
		{
			ArgumentNullException.ThrowIfNull(queryContext);

			var parameter = Expression.Parameter(typeof(TChild), "child");
			Expression<Func<TChild, TChild>>? selector = null;
			if (queryContext.Selector != null)
			{
				Expression<Func<TParent, Func<TChild>, TChild>> upcast = (parent, fallback) => (parent as TChild) ?? fallback();
				selector = Expression.Lambda<Func<TChild, TChild>>(
					Expression.Invoke(
						upcast,
						Expression.Invoke(
							queryContext.Selector,
							parameter),
						fallback),
					parameter);
			}

			Expression<Func<TChild, bool>>? predicate = null;
			if (queryContext.Predicate != null)
				predicate = Expression.Lambda<Func<TChild, bool>>(
					queryContext.Predicate,
					parameter);

			SortDefinition<TChild>? sortDefinition = null;
			if (queryContext.Sorting?.Operations.Length > 0)
				throw new NotImplementedException();

			return new QueryContext<TChild>(
				selector,
				predicate,
				sortDefinition);
		}

		/// <summary>
		/// Translate a given <paramref name="queryContext"/> into one with the target wrapped in an <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The result <see cref="Type"/> of the <paramref name="queryContext"/>.</typeparam>
		/// <param name="queryContext">The original <see cref="QueryContext{TEntity}"/>.</param>
		/// <returns>A <see cref="QueryContext{TEntity}"/> with the target wrapped in an <see cref="AuthorityResponse{TResult}"/>.</returns>
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
					AuthorityResponse<TResult>.MappingExpression,
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

		/// <summary>
		/// Unwrap a given <paramref name="queryContext"/> that was built from one resulting from <see cref="AuthorityResponseWrap{TResult}(QueryContext{TResult})"/>.
		/// </summary>
		/// <typeparam name="TResult">The underlying result <see cref="Type"/> of the <paramref name="queryContext"/>.</typeparam>
		/// <param name="queryContext">The wrapped <see cref="QueryContext{TEntity}"/> to unwrap.</param>
		/// <returns>The unwrapped <see cref="QueryContext{TEntity}"/> for <typeparamref name="TResult"/>.</returns>
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
