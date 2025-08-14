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
