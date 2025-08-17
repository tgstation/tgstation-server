using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GreenDonut.Data;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IQueryable{T}"/>.
	/// </summary>
	static class QueryableExtensions
	{
		/// <summary>
		/// Map a given <paramref name="queryContext"/> to project onto a given <paramref name="queryable"/>.
		/// </summary>
		/// <typeparam name="TQueried">The <see cref="ProjectedPair{TQueried, TResult}.Queried"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TResult">The <see cref="ProjectedPair{TQueried, TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <param name="queryable">A <see cref="IQueryable{T}"/> for <see cref="ProjectedPair{TQueried, TResult}"/>s of <typeparamref name="TQueried"/>/<typeparamref name="TResult"/>.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for <typeparamref name="TResult"/> to map onto the <paramref name="queryable"/>.</param>
		/// <returns>A new <see cref="IQueryable{T}"/> with <paramref name="queryContext"/> mapped on the <typeparamref name="TResult"/>.</returns>
		public static IQueryable<ProjectedPair<TQueried, TResult>> With<TQueried, TResult>(
			this IQueryable<ProjectedPair<TQueried, TResult>> queryable,
			QueryContext<TResult>? queryContext)
		{
			// taken verbatim from GreenDonut's With implementation
			ArgumentNullException.ThrowIfNull(queryable);

			if (queryContext == null)
				return queryable;

			var modified = false;
			if (queryContext.Predicate != null)
			{
				queryable = queryable.Where(
					TranslateLambda<TQueried, TResult, bool>(
						queryContext.Predicate));
				modified = true;
			}

			if (queryContext.Sorting?.Operations.Length > 0)
			{
				var definition = new SortDefinition<ProjectedPair<TQueried, TResult>>(
					queryContext.Sorting.Operations
						.Select(MapSortBy<TQueried, TResult>));

				queryable = queryable.OrderBy(definition);
				modified = true;
			}

			if (queryContext.Selector != null)
			{
				Expression<Func<ProjectedPair<TQueried, TResult>, TResult, ProjectedPair<TQueried, TResult>>> remapSelector =
					(initialProjected, selectedResult) => new ProjectedPair<TQueried, TResult>
					{
						Queried = initialProjected.Queried,
						Result = selectedResult,
					};
				Expression<Func<ProjectedPair<TQueried, TResult>, TResult>> resultSelector = projected => projected.Result;
				var parameter = Expression.Parameter(typeof(ProjectedPair<TQueried, TResult>), "projectedParam");
				var result = Expression.Parameter(typeof(TResult), "resultParam");
				var initialResultExpr = Expression.Invoke(resultSelector, parameter);
				var resultExpr = Expression.Invoke(queryContext.Selector, initialResultExpr);
				var finalInvoke = Expression.Invoke(remapSelector, parameter, resultExpr);
				var finalExpr = Expression.Lambda<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<TQueried, TResult>>>(finalInvoke, parameter);

				queryable = queryable.Select(finalExpr);
				modified = true;
			}

			if (modified)
				queryable = queryable
					.TagWith("GraphQL Projections Applied");

			return queryable;
		}

		/// <summary>
		/// Map a given <paramref name="sortBy"/> onto a <see cref="ProjectedPair{TQueried, TResult}"/>.
		/// </summary>
		/// <typeparam name="TQueried">The <see cref="ProjectedPair{TQueried, TResult}.Queried"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TResult">The <see cref="ProjectedPair{TQueried, TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <param name="sortBy">The <see cref="ISortBy{TEntity}"/> for <typeparamref name="TResult"/> to map onto a <see cref="ProjectedPair{TQueried, TResult}"/> of <typeparamref name="TQueried"/>/<typeparamref name="TResult"/>.</param>
		/// <returns>A new <see cref="ISortBy{TEntity}"/> for the <see cref="ProjectedPair{TQueried, TResult}"/>.</returns>
		private static ISortBy<ProjectedPair<TQueried, TResult>> MapSortBy<TQueried, TResult>(ISortBy<TResult> sortBy)
		{
			var sortingKeyType = sortBy.GetType().GenericTypeArguments[1];

			var factoryMethod = typeof(SortBy<ProjectedPair<TQueried, TResult>>)
				.GetMethod(
					sortBy.Ascending
						? nameof(SortBy<ProjectedPair<TQueried, TResult>>.Ascending)
						: nameof(SortBy<ProjectedPair<TQueried, TResult>>.Descending))!;

			var instantiatedLambdaTranslate = typeof(QueryableExtensions)
				.GetMethod(nameof(TranslateLambda), BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(typeof(TQueried), typeof(TResult), sortingKeyType);

			var fixedLambda = instantiatedLambdaTranslate.Invoke(null, [sortBy.KeySelector])!;

			var instantiatedFactoryMethod = factoryMethod.MakeGenericMethod(sortingKeyType);

			return (ISortBy<ProjectedPair<TQueried, TResult>>)instantiatedFactoryMethod.Invoke(null, [fixedLambda])!;
		}

		/// <summary>
		/// Translate a given <paramref name="selectionExpression"/> on a <typeparamref name="TResult"/> onto its <see cref="ProjectedPair{TQueried, TResult}"/>.
		/// </summary>
		/// <typeparam name="TQueried">The <see cref="ProjectedPair{TQueried, TResult}.Queried"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TResult">The <see cref="ProjectedPair{TQueried, TResult}.Result"/> <see cref="Type"/>.</typeparam>
		/// <typeparam name="TDesired">The selected <see cref="Type"/>.</typeparam>
		/// <param name="selectionExpression">A <see cref="LambdaExpression"/> accepting a <typeparamref name="TResult"/> and outputting a <typeparamref name="TDesired"/>.</param>
		/// <returns>An <see cref="Expression{TDelegate}"/> accepting a <see cref="ProjectedPair{TQueried, TResult}"/> of <typeparamref name="TQueried"/>/<typeparamref name="TResult"/> and outputting a <typeparamref name="TDesired"/>.</returns>
		private static Expression<Func<ProjectedPair<TQueried, TResult>, TDesired>> TranslateLambda<TQueried, TResult, TDesired>(LambdaExpression selectionExpression)
		{
			Expression<Func<ProjectedPair<TQueried, TResult>, TResult>> resultSelector = projected => projected.Result;

			var parameter = Expression.Parameter(typeof(ProjectedPair<TQueried, TResult>), "projectedTranslateParam");
			var result = Expression.Invoke(resultSelector, parameter);
			var expr = Expression.Invoke(selectionExpression, result);

			return Expression.Lambda<Func<ProjectedPair<TQueried, TResult>, TDesired>>(expr, parameter);
		}
	}
}
