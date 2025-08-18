using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for transforming collections.
	/// </summary>
	/// <typeparam name="TInput">The input collection's underlying <see cref="Type"/>.</typeparam>
	/// <typeparam name="TOutput">The output collection's underlying <see cref="Type"/>.</typeparam>
	/// <typeparam name="TTransformer">The <see cref="ITransformer{TInput, TOutput}"/> <see cref="Type"/> from <typeparamref name="TInput"/> to <typeparamref name="TOutput"/>.</typeparam>
	sealed class CollectionTransformer<TInput, TOutput, TTransformer> : TransformerBase<IEnumerable<TInput>, List<TOutput>>
		where TTransformer : ITransformer<TInput, TOutput>, new()
	{
		/// <summary>
		/// Build the <see cref="TransformerBase{TInput, TOutput}.Expression"/>.
		/// </summary>
		/// <returns>The <see cref="Expression{TDelegate}"/> for converting collections of <typeparamref name="TInput"/>s to <typeparamref name="TOutput"/>s.</returns>
		private static Expression<Func<IEnumerable<TInput>, List<TOutput>>> BuildExpression()
		{
			var subTransformer = new TTransformer();
			Expression<Func<IEnumerable<TInput>, Func<TInput, TOutput>, List<TOutput>>> expression = (model, subExpression) => model
				.Select(subExpression)
				.ToList();

			var parameter = global::System.Linq.Expressions.Expression.Parameter(typeof(IEnumerable<TInput>), "collectionInput");
			var invocation = global::System.Linq.Expressions.Expression.Invoke(expression, parameter, subTransformer.Expression);

			var lambda = global::System.Linq.Expressions.Expression.Lambda<Func<IEnumerable<TInput>, List<TOutput>>>(invocation, parameter);

			return lambda;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionTransformer{TInput, TOutput, TTransformer}"/> class.
		/// </summary>
		public CollectionTransformer()
			: base(BuildExpression())
		{
		}
	}
}
