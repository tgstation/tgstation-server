namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a host-side model that may be transformed into a <typeparamref name="TApiModel"/>.
	/// </summary>
	/// <typeparam name="TApiModel">The API form of the model.</typeparam>
	public interface ILegacyApiTransformable<out TApiModel>
	{
		/// <summary>
		/// Convert the <see cref="ILegacyApiTransformable{TApiModel}"/> to it's <typeparamref name="TApiModel"/>.
		/// </summary>
		/// <returns>A new <typeparamref name="TApiModel"/> based on the <see cref="ILegacyApiTransformable{TApiModel}"/>.</returns>
		TApiModel ToApi();
	}
}
