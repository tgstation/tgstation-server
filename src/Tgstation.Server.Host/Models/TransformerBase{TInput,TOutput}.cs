using System;
using System.Linq.Expressions;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Models
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
		static Expression<Func<TInput, ProjectedPair<TInput, TOutput>>>? projectedExpression;

		/// <inheritdoc />
		public Expression<Func<TInput, TOutput>> Expression { get; }

		/// <inheritdoc />
		public Expression<Func<TInput, ProjectedPair<TInput, TOutput>>> ProjectedExpression { get; }

		/// <inheritdoc />
		public Func<TInput, TOutput> CompiledExpression { get; }

		/// <summary>
		/// Gets the <typeparamref name="T"/> that should be used when a database projected non-null DTO value is null in the expression.
		/// </summary>
		/// <typeparam name="T">The non-null <see cref="Type"/> a fallback is required for.</typeparam>
		/// <returns>A fallback <typeparamref name="T"/> value.</returns>
		protected static T NotNullFallback<T>()
			where T : notnull
			=> default!;

		/// <summary>
		/// Build an <see cref="Expression{TDelegate}"/> for <typeparamref name="TInput"/> to <typeparamref name="TOutput"/> when <typeparamref name="TInput"/> contains a <typeparamref name="TSubInput"/> with its own <typeparamref name="TTransformer"/>.
		/// </summary>
		/// <typeparam name="TSubInput">The field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubOutput">The transformed <see cref="Type"/> of <typeparamref name="TSubInput"/>.</typeparam>
		/// <typeparam name="TTransformer">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput"/>/<typeparamref name="TSubOutput"/>.</typeparam>
		/// <param name="transformerExpression">The <see cref="Expression{TDelegate}"/> to take a <typeparamref name="TInput"/> and <typeparamref name="TSubOutput"/> and produce a <typeparamref name="TOutput"/>.</param>
		/// <param name="subInputSelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput"/> from <typeparamref name="TInput"/>.</param>
		/// <returns>An expression converting <typeparamref name="TInput"/> into <typeparamref name="TOutput"/> based on <paramref name="transformerExpression"/> with its other arguments generated from the transformation result of <paramref name="subInputSelectionExpression"/>.</returns>
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

		/// <summary>
		/// Build an <see cref="Expression{TDelegate}"/> for <typeparamref name="TInput"/> to <typeparamref name="TOutput"/> when <typeparamref name="TInput"/> contains two sub-inputs with their own <see cref="ITransformer{TInput, TOutput}"/>s.
		/// </summary>
		/// <typeparam name="TSubInput1">The first field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubInput2">The second field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubOutput1">The first transformed <see cref="Type"/> of <typeparamref name="TSubInput1"/>.</typeparam>
		/// <typeparam name="TSubOutput2">The second transformed <see cref="Type"/> of <typeparamref name="TSubInput2"/>.</typeparam>
		/// <typeparam name="TTransformer1">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput1"/>/<typeparamref name="TSubOutput2"/>.</typeparam>
		/// <typeparam name="TTransformer2">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput2"/>/<typeparamref name="TSubOutput2"/>.</typeparam>
		/// <param name="transformerExpression">The <see cref="Expression{TDelegate}"/> to take a <typeparamref name="TInput"/>, <typeparamref name="TSubOutput1"/>, and <typeparamref name="TSubOutput1"/> and produce a <typeparamref name="TOutput"/>.</param>
		/// <param name="subInput1SelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput1"/> from <typeparamref name="TInput"/>.</param>
		/// <param name="subInput2SelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput2"/> from <typeparamref name="TInput"/>.</param>
		/// <returns>An expression converting <typeparamref name="TInput"/> into <typeparamref name="TOutput"/> based on <paramref name="transformerExpression"/> with its other arguments generated from the transformation result of <paramref name="subInput1SelectionExpression"/> and <paramref name="subInput2SelectionExpression"/>.</returns>
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
