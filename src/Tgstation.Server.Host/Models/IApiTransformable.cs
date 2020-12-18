namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a host-side model that may be transformed into a <typeparamref name="TApiModel"/>.
	/// </summary>
	/// <typeparam name="TApiModel">The API form of the model.</typeparam>
	public interface IApiTransformable<TApiModel>
	{
		/// <summary>
		/// Convert the <see cref="IApiTransformable{TApiModel}"/> to it's <typeparamref name="TApiModel"/>.
		/// </summary>
		/// <returns>A new <typeparamref name="TApiModel"/> based on the <see cref="IApiTransformable{TApiModel}"/>.</returns>
		TApiModel ToApi();
	}
}
