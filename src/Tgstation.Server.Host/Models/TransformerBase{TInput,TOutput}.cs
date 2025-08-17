using System;
using System.Diagnostics;
using System.Linq.Expressions;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	abstract class TransformerBase<TInput, TOutput> : ITransformer<TInput, TOutput>
	{
		/// <summary>
		/// <see langword="static"/> cache for compiling the <see cref="Expression{TDelegate}"/> we were constructed with.
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
			=> BuildSubProjectionN(
				transformerExpression,
				[
					typeof(TSubInput),
				],
				[
					typeof(TSubOutput),
				],
				[
					subInputSelectionExpression,
				],
				[
					new TTransformer().Expression,
				]);

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
			=> BuildSubProjectionN(
				transformerExpression,
				[
					typeof(TSubInput1),
					typeof(TSubInput2),
				],
				[
					typeof(TSubOutput1),
					typeof(TSubOutput2),
				],
				[
					subInput1SelectionExpression,
					subInput2SelectionExpression,
				],
				[
					new TTransformer1().Expression,
					new TTransformer2().Expression,
				]);

		/// <summary>
		/// Build an <see cref="Expression{TDelegate}"/> for <typeparamref name="TInput"/> to <typeparamref name="TOutput"/> when <typeparamref name="TInput"/> contains three sub-inputs with their own <see cref="ITransformer{TInput, TOutput}"/>s.
		/// </summary>
		/// <typeparam name="TSubInput1">The first field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubInput2">The second field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubInput3">The third field <see cref="Type"/> in <typeparamref name="TInput"/> that needs transforming.</typeparam>
		/// <typeparam name="TSubOutput1">The first transformed <see cref="Type"/> of <typeparamref name="TSubInput1"/>.</typeparam>
		/// <typeparam name="TSubOutput2">The second transformed <see cref="Type"/> of <typeparamref name="TSubInput2"/>.</typeparam>
		/// <typeparam name="TSubOutput3">The third transformed <see cref="Type"/> of <typeparamref name="TSubInput2"/>.</typeparam>
		/// <typeparam name="TTransformer1">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput1"/>/<typeparamref name="TSubOutput2"/>.</typeparam>
		/// <typeparam name="TTransformer2">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput2"/>/<typeparamref name="TSubOutput2"/>.</typeparam>
		/// <typeparam name="TTransformer3">The <see cref="ITransformer{TInput, TOutput}"/> for <typeparamref name="TSubInput3"/>/<typeparamref name="TSubOutput3"/>.</typeparam>
		/// <param name="transformerExpression">The <see cref="Expression{TDelegate}"/> to take a <typeparamref name="TInput"/>, <typeparamref name="TSubOutput1"/>, and <typeparamref name="TSubOutput1"/> and produce a <typeparamref name="TOutput"/>.</param>
		/// <param name="subInput1SelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput1"/> from <typeparamref name="TInput"/>.</param>
		/// <param name="subInput2SelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput2"/> from <typeparamref name="TInput"/>.</param>
		/// <param name="subInput3SelectionExpression">The <see cref="Expression{TDelegate}"/> to select <typeparamref name="TSubInput3"/> from <typeparamref name="TInput"/>.</param>
		/// <returns>An expression converting <typeparamref name="TInput"/> into <typeparamref name="TOutput"/> based on <paramref name="transformerExpression"/> with its other arguments generated from the transformation result of <paramref name="subInput1SelectionExpression"/>, <paramref name="subInput2SelectionExpression"/>, <paramref name="subInput3SelectionExpression"/>.</returns>
		protected static Expression<Func<TInput, TOutput>> BuildSubProjection<
			TSubInput1,
			TSubInput2,
			TSubInput3,
			TSubOutput1,
			TSubOutput2,
			TSubOutput3,
			TTransformer1,
			TTransformer2,
			TTransformer3>(
			Expression<Func<TInput, TSubOutput1?, TSubOutput2?, TSubOutput3?, TOutput>> transformerExpression,
			Expression<Func<TInput, TSubInput1?>> subInput1SelectionExpression,
			Expression<Func<TInput, TSubInput2?>> subInput2SelectionExpression,
			Expression<Func<TInput, TSubInput3?>> subInput3SelectionExpression)
			where TSubOutput1 : class
			where TSubOutput2 : class
			where TSubOutput3 : class
			where TTransformer1 : ITransformer<TSubInput1, TSubOutput1>, new()
			where TTransformer2 : ITransformer<TSubInput2, TSubOutput2>, new()
			where TTransformer3 : ITransformer<TSubInput3, TSubOutput3>, new()
			=> BuildSubProjectionN(
				transformerExpression,
				[
					typeof(TSubInput1),
					typeof(TSubInput2),
					typeof(TSubInput3),
				],
				[
					typeof(TSubOutput1),
					typeof(TSubOutput2),
					typeof(TSubOutput3),
				],
				[
					subInput1SelectionExpression,
					subInput2SelectionExpression,
					subInput3SelectionExpression,
				],
				[
					new TTransformer1().Expression,
					new TTransformer2().Expression,
					new TTransformer3().Expression,
				]);

		/// <summary>
		/// Build an <see cref="Expression{TDelegate}"/> for <typeparamref name="TInput"/> to <typeparamref name="TOutput"/> when <typeparamref name="TInput"/> contains N sub-inputs with their own <see cref="ITransformer{TInput, TOutput}"/>s.
		/// </summary>
		/// <param name="transformerExpression">The <see cref="Expression{TDelegate}"/> to take a <typeparamref name="TInput"/> and transformed sub-outputs and produce a <typeparamref name="TOutput"/>.</param>
		/// <param name="subInputTypes">The <see cref="Type"/>s in <typeparamref name="TInput"/> that need transforming.</param>
		/// <param name="subOutputTypes">The transformed <paramref name="subInputTypes"/>.</param>
		/// <param name="subInputSelectionExpressions"><see cref="LambdaExpression"/>s to select the <paramref name="subInputTypes"/> from <typeparamref name="TInput"/>.</param>
		/// <param name="subInputTransformerExpressions"><see cref="LambdaExpression"/>s to transform the <paramref name="subInputTypes"/> into <paramref name="subOutputTypes"/>.</param>
		/// <returns>An expression converting <typeparamref name="TInput"/> into <typeparamref name="TOutput"/> based on <paramref name="transformerExpression"/> with its other arguments generated from the transformation result of <paramref name="subInputSelectionExpressions"/>.</returns>
		private static Expression<Func<TInput, TOutput>> BuildSubProjectionN(
			LambdaExpression transformerExpression,
			Type[] subInputTypes,
			Type[] subOutputTypes,
			LambdaExpression[] subInputSelectionExpressions,
			LambdaExpression[] subInputTransformerExpressions)
		{
			var n = subInputTypes.Length;

			Debug.Assert(n == subOutputTypes.Length, $"{nameof(subOutputTypes)}.{nameof(Array.Length)} != {nameof(subInputTypes)}.{nameof(Array.Length)}");
			Debug.Assert(n == subInputSelectionExpressions.Length, $"{nameof(subInputSelectionExpressions)}.{nameof(Array.Length)} != n");
			Debug.Assert(n == subInputTransformerExpressions.Length, $"{nameof(subInputTransformerExpressions)}.{nameof(Array.Length)} != n");

			var primaryInput = global::System.Linq.Expressions.Expression.Parameter(typeof(TInput), "input");

			var finalExpressionParameters = new Expression[n + 1];
			finalExpressionParameters[0] = primaryInput;

			for (int i = 0; i < n; i++)
			{
				var subInputExpression = global::System.Linq.Expressions.Expression.Invoke(subInputSelectionExpressions[i], primaryInput);

				var notNullExpression = global::System.Linq.Expressions.Expression.MakeBinary(
					ExpressionType.NotEqual,
					subInputExpression,
					global::System.Linq.Expressions.Expression.Constant(null, subInputTypes[i]));

				var subOutputExpression = global::System.Linq.Expressions.Expression.Invoke(subInputTransformerExpressions[i], subInputExpression);

				var conditionalSubOutputExpression = global::System.Linq.Expressions.Expression.Condition(
					notNullExpression,
					subOutputExpression,
					global::System.Linq.Expressions.Expression.Constant(null, subOutputTypes[i]));

				finalExpressionParameters[i + 1] = conditionalSubOutputExpression;
			}

			var outputExpression = global::System.Linq.Expressions.Expression.Invoke(
				transformerExpression,
				finalExpressionParameters);

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
			ProjectedExpression = projectedExpression;
		}

		/// <inheritdoc />
		public TOutput Transform(TInput input)
			=> compiledExpression!(input);
	}
}
