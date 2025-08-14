using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GreenDonut.Data;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IQueryable{T}"/>.
	/// </summary>
	static class QueryableExtensions
	{
		public static IQueryable<Projected<TQueried, TResult>> With<TQueried, TResult>(this IQueryable<Projected<TQueried, TResult>> queryable, QueryContext<TResult>? queryContext)
		{
			// taken verbatim from GreenDonut's With implementation
			ArgumentNullException.ThrowIfNull(queryable);

			if (queryContext == null)
				return queryable;

			if (queryContext.Predicate != null)
				queryable = queryable.Where(
					TranslateGenericLambda<TQueried, TResult, bool>(
						queryContext.Predicate));

			if (queryContext.Sorting?.Operations.Length > 0)
			{
				var definition = new SortDefinition<Projected<TQueried, TResult>>(
					queryContext.Sorting.Operations
						.Select(MapSortBy<TQueried, TResult>));

				queryable = queryable.OrderBy(definition);
			}

			if (queryContext.Selector != null)
			{
				Expression<Func<Projected<TQueried, TResult>, TResult, Projected<TQueried, TResult>>> remapSelector =
					(initialProjected, selectedResult) => new Projected<TQueried, TResult>
					{
						Queried = initialProjected.Queried,
						Result = selectedResult,
					};
				Expression<Func<Projected<TQueried, TResult>, TResult>> resultSelector = projected => projected.Result;
				var parameter = Expression.Parameter(typeof(Projected<TQueried, TResult>), "projectedParam");
				var result = Expression.Parameter(typeof(TResult), "resultParam");
				var initialResultExpr = Expression.Invoke(resultSelector, parameter);
				var resultExpr = Expression.Invoke(queryContext.Selector, initialResultExpr);
				var finalInvoke = Expression.Invoke(remapSelector, parameter, resultExpr);
				var finalExpr = Expression.Lambda<Func<Projected<TQueried, TResult>, Projected<TQueried, TResult>>>(finalInvoke, parameter);

				queryable = queryable.Select(finalExpr);
			}

			return queryable;
		}

		private static ISortBy<Projected<TQueried, TResult>> MapSortBy<TQueried, TResult>(ISortBy<TResult> sortBy)
		{
			var sortingKeyType = sortBy.GetType().GenericTypeArguments[1];

			var factoryMethod = typeof(SortBy<Projected<TQueried, TResult>>)
				.GetMethod(
					sortBy.Ascending
						? nameof(SortBy<Projected<TQueried, TResult>>.Ascending)
						: nameof(SortBy<Projected<TQueried, TResult>>.Descending))!;

			var instantiatedLambdaTranslate = typeof(QueryableExtensions)
				.GetMethod(nameof(TranslateLambda), BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(typeof(TQueried), typeof(TResult), sortingKeyType);

			var fixedLambda = instantiatedLambdaTranslate.Invoke(null, [sortBy.KeySelector])!;

			var instantiatedFactoryMethod = factoryMethod.MakeGenericMethod(sortingKeyType);

			return (ISortBy<Projected<TQueried, TResult>>)instantiatedFactoryMethod.Invoke(null, [fixedLambda])!;
		}

		private static Expression<Func<Projected<TQueried, TResult>, TDesired>> TranslateGenericLambda<TQueried, TResult, TDesired>(Expression<Func<TResult, TDesired>> selectionExpression)
			=> TranslateLambda<TQueried, TResult, TDesired>(selectionExpression);

		private static Expression<Func<Projected<TQueried, TResult>, TDesired>> TranslateLambda<TQueried, TResult, TDesired>(LambdaExpression selectionExpression)
		{
			Expression<Func<Projected<TQueried, TResult>, TResult>> resultSelector = projected => projected.Result;

			var parameter = Expression.Parameter(typeof(Projected<TQueried, TResult>), "projectedTranslateParam");
			var result = Expression.Invoke(resultSelector, parameter);
			var expr = Expression.Invoke(selectionExpression, result);

			return Expression.Lambda<Func<Projected<TQueried, TResult>, TDesired>>(expr, parameter);
		}
	}
}
