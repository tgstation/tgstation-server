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
		public static Expression<Func<TQueried, Projected<TQueried, TResult>>> Projected<TQueried, TResult>(this Expression<Func<TQueried, TResult>> translationExpression)
		{
			var parameter = Expression.Parameter(typeof(TQueried), "queried");
			var body = Expression.Invoke(translationExpression, parameter);

			var ourType = typeof(Projected<TQueried, TResult>);

			var expr = Expression.MemberInit(
				Expression.New(ourType),
				Expression.Bind(
					ourType.GetProperty(nameof(Authority.Core.Projected<TQueried, TResult>.Queried))!,
					parameter),
				Expression.Bind(
					ourType.GetProperty(nameof(Authority.Core.Projected<TQueried, TResult>.Result))!,
					body));

			return Expression.Lambda<Func<TQueried, Projected<TQueried, TResult>>>(expr, parameter);
		}
	}
}
