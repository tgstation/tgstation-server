using System;
using System.Linq.Expressions;
using System.Reflection;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Extensions for <see cref="Models"/>.
	/// </summary>
	static class ModelExtensions
	{
		/// <summary>
		/// Require a given <see cref="Nullable{T}"/> property of a given <paramref name="model"/> be non-<see langword="null"/>.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of the <paramref name="model"/> being accessed.</typeparam>
		/// <typeparam name="TProperty">The <see cref="Type"/> of the property being accessed.</typeparam>
		/// <param name="model">The <typeparamref name="TModel"/>.</param>
		/// <param name="accessor">The <typeparamref name="TProperty"/> access <see cref="Expression{TDelegate}"/>.</param>
		/// <returns>The value of <typeparamref name="TProperty"/> in <paramref name="model"/>.</returns>
		/// <exception cref="InvalidOperationException">When <typeparamref name="TProperty"/> in <paramref name="model"/> is <see langword="null"/>.</exception>
		public static TProperty Require<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty?>> accessor)
			where TModel : EntityId
			where TProperty : struct
		{
			ArgumentNullException.ThrowIfNull(model);
			ArgumentNullException.ThrowIfNull(accessor);

			var memberSelectorExpression = (MemberExpression)accessor.Body;
			var property = (PropertyInfo)memberSelectorExpression.Member;

			var nullableValue = (TProperty?)property.GetValue(model);
			if (!nullableValue.HasValue)
				throw new InvalidOperationException($"Expected {model.GetType().Name}.{property.Name} to be set here!");

			return nullableValue.Value;
		}
	}
}
