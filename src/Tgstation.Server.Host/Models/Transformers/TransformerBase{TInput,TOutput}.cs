using System;
using System.Linq.Expressions;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <inheritdoc />
	abstract class TransformerBase<TInput, TOutput> : ITransformer<TInput, TOutput>
	{
		/// <summary>
		/// <see langword="static"/> cache for <see cref="CompiledExpression"/>.
		/// </summary>
		static Func<TInput, TOutput>? compiledExpression; // This is safe https://stackoverflow.com/a/9647661/3976486

		/// <inheritdoc />
		public Expression<Func<TInput, TOutput>> Expression { get; }

		/// <inheritdoc />
		public Func<TInput, TOutput> CompiledExpression { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformerBase{TInput, TOutput}"/> class.
		/// </summary>
		/// <param name="expression">The value of <see cref="Expression"/>.</param>
		protected TransformerBase(
			Expression<Func<TInput, TOutput>> expression)
		{
			compiledExpression ??= expression.Compile();
			Expression = expression;
			CompiledExpression = compiledExpression;
		}
	}
}
