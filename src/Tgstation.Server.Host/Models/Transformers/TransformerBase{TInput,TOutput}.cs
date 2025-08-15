using System;
using System.Linq.Expressions;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <inheritdoc />
	abstract class TransformerBase<TInput, TOutput> : ITransformer<TInput, TOutput>
	{
		/// <summary>
		/// <see langword="static"/> cache for <see cref="CompiledExpression"/>.
		/// </summary>
		static Func<TInput, TOutput>? compiledExpression; // This is safe https://stackoverflow.com/a/9647661/3976486

		/// <summary>
		/// <see langword="static"/> cache for <see cref="ProjectedExpression"/>.
		/// </summary>
		static Expression<Func<TInput, Projected<TInput, TOutput>>>? projectedExpression;

		/// <inheritdoc />
		public Expression<Func<TInput, TOutput>> Expression { get; }

		/// <inheritdoc />
		public Expression<Func<TInput, Projected<TInput, TOutput>>> ProjectedExpression { get; }

		/// <inheritdoc />
		public Func<TInput, TOutput> CompiledExpression { get; }

		protected static T NotNullFallback<T>()
			where T : notnull
			=> default!;

		protected static Expression<Func<TInput, TOutput>> BuildSubProjection<TSubInput, TSubOutput, TTransformer>(
			Expression<Func<TInput, TSubOutput?, TOutput>> transformerExpression,
			Expression<Func<TInput, TSubInput?>> subInputSelectionExpression)
			where TSubOutput : class
			where TTransformer : ITransformer<TSubInput, TSubOutput>, new()
		{
			var subTransformer = new TTransformer();

			var primaryInput = global::System.Linq.Expressions.Expression.Parameter(typeof(TInput), "input");
			var subInputExpression = global::System.Linq.Expressions.Expression.Invoke(subInputSelectionExpression, primaryInput);

			var notNullExpression = global::System.Linq.Expressions.Expression.MakeBinary(
				ExpressionType.NotEqual,
				subInputExpression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubInput)));

			var subOutputExpression = global::System.Linq.Expressions.Expression.Invoke(subTransformer.Expression, subInputExpression);

			var conditionalSubOutputExpression = global::System.Linq.Expressions.Expression.Condition(
				notNullExpression,
				subOutputExpression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubOutput)));

			var outputExpression = global::System.Linq.Expressions.Expression.Invoke(transformerExpression, primaryInput, conditionalSubOutputExpression);

			return global::System.Linq.Expressions.Expression.Lambda<Func<TInput, TOutput>>(outputExpression, primaryInput);
		}

		protected static Expression<Func<TInput, TOutput>> BuildSubProjection<
			TSubInput1,
			TSubInput2,
			TSubOutput1,
			TSubOutput2,
			TTransformer1,
			TTransformer2>(
			Expression<Func<TInput, TSubOutput1?, TSubOutput2?, TOutput>> transformerExpression,
			Expression<Func<TInput, TSubInput1?>> subInput1SelectionExpression,
			Expression<Func<TInput, TSubInput2?>> subInput2SelectionExpression)
			where TSubOutput1 : class
			where TSubOutput2 : class
			where TTransformer1 : ITransformer<TSubInput1, TSubOutput1>, new()
			where TTransformer2 : ITransformer<TSubInput2, TSubOutput2>, new()
		{
			var subTransformer1 = new TTransformer1();
			var subTransformer2 = new TTransformer2();

			var primaryInput = global::System.Linq.Expressions.Expression.Parameter(typeof(TInput), "input");

			var subInput1Expression = global::System.Linq.Expressions.Expression.Invoke(subInput1SelectionExpression, primaryInput);
			var subInput2Expression = global::System.Linq.Expressions.Expression.Invoke(subInput2SelectionExpression, primaryInput);

			var notNullExpression1 = global::System.Linq.Expressions.Expression.MakeBinary(
				ExpressionType.NotEqual,
				subInput1Expression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubInput1)));
			var notNullExpression2 = global::System.Linq.Expressions.Expression.MakeBinary(
				ExpressionType.NotEqual,
				subInput2Expression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubInput2)));

			var subOutput1Expression = global::System.Linq.Expressions.Expression.Invoke(subTransformer1.Expression, subInput1Expression);
			var subOutput2Expression = global::System.Linq.Expressions.Expression.Invoke(subTransformer2.Expression, subInput2Expression);

			var conditionalSubOutput1Expression = global::System.Linq.Expressions.Expression.Condition(
				notNullExpression1,
				subOutput1Expression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubOutput1)));

			var conditionalSubOutput2Expression = global::System.Linq.Expressions.Expression.Condition(
				notNullExpression2,
				subOutput2Expression,
				global::System.Linq.Expressions.Expression.Constant(null, typeof(TSubOutput2)));

			var outputExpression = global::System.Linq.Expressions.Expression.Invoke(transformerExpression, primaryInput, conditionalSubOutput1Expression, conditionalSubOutput2Expression);

			return global::System.Linq.Expressions.Expression.Lambda<Func<TInput, TOutput>>(outputExpression, primaryInput);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerBase{TInput, TOutput}"/> class.
		/// </summary>
		/// <param name="expression">The value of <see cref="Expression"/>.</param>
		protected TransformerBase(
			Expression<Func<TInput, TOutput>> expression)
		{
			compiledExpression ??= expression.Compile();
			projectedExpression ??= expression.Projected();
			Expression = expression;
			CompiledExpression = compiledExpression;
			ProjectedExpression = projectedExpression;
		}
	}
}
