using System;

#pragma warning disable CA1005

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a host-side model that may be transformed into a <typeparamref name="TApiModel"/>.
	/// </summary>
	/// <typeparam name="TModel">The internal model <see cref="Type"/>.</typeparam>
	/// <typeparam name="TApiModel">The API model <see cref="Type"/>.</typeparam>
	/// <typeparam name="TTransformer">The <see cref="ITransformer{TModel, TApiModel}"/> <see cref="Type"/>.</typeparam>
	public interface IApiTransformable<TModel, TApiModel, TTransformer>
		where TApiModel : notnull
		where TModel : IApiTransformable<TModel, TApiModel, TTransformer>
		where TTransformer : ITransformer<TModel, TApiModel>, new()
	{
		/// <summary>
		/// Convert the <see cref="IApiTransformable{TModel, TApiModel, TApiTransformer}"/> to it's <typeparamref name="TApiModel"/>.
		/// </summary>
		/// <returns>A new <typeparamref name="TApiModel"/> based on the <see cref="IApiTransformable{TModel, TApiModel, TApiTransformer}"/>.</returns>
		TApiModel ToApi()
			=> new TTransformer()
				.CompiledExpression((TModel)this);
	}
}
