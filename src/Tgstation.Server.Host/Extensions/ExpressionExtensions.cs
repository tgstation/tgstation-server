using System;
using System.Linq.Expressions;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="Expression{TDelegate}"/>s.
	/// </summary>
	static class ExpressionExtensions
	{
		/// <summary>
		/// Create an <see cref="Expression{TDelegate}"/> to transform a given <typeparamref name="TQueried"/> into a <typeparamref name="TResult"/> and output them as a <see cref="ProjectedPair{TQueried, TResult}"/>.
		/// </summary>
		/// <typeparam name="TQueried">The input <see cref="Type"/>.</typeparam>
		/// <typeparam name="TResult">The output <see cref="Type"/>.</typeparam>
		/// <param name="translationExpression">An <see cref="Expression{TDelegate}"/> to transform a given <typeparamref name="TQueried"/> into a <typeparamref name="TResult"/>.</param>
		/// <returns>An <see cref="Expression{TDelegate}"/> to transform a given <typeparamref name="TQueried"/> into a <typeparamref name="TResult"/> and output them as a <see cref="ProjectedPair{TQueried, TResult}"/>.</returns>
		public static Expression<Func<TQueried, ProjectedPair<TQueried, TResult>>> Projected<TQueried, TResult>(this Expression<Func<TQueried, TResult>> translationExpression)
		{
			var parameter = Expression.Parameter(typeof(TQueried), "queried");
			var body = Expression.Invoke(translationExpression, parameter);

			var ourType = typeof(ProjectedPair<TQueried, TResult>);

			var expr = Expression.MemberInit(
				Expression.New(ourType),
				Expression.Bind(
					ourType.GetProperty(nameof(ProjectedPair<TQueried, TResult>.Queried))!,
					parameter),
				Expression.Bind(
					ourType.GetProperty(nameof(ProjectedPair<TQueried, TResult>.Result))!,
					body));

			return Expression.Lambda<Func<TQueried, ProjectedPair<TQueried, TResult>>>(expr, parameter);
		}
	}
}
