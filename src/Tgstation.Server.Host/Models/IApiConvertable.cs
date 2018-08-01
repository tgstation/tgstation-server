namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// For converting models to their API form
	/// </summary>
	/// <typeparam name="TModel">Which of the <see cref="Api.Models"/> this model backs</typeparam>
	public interface IApiConvertable<TModel> where TModel : class
	{
		/// <summary>
		/// Convert the model to it's API form
		/// </summary>
		/// <returns>A new <typeparamref name="TModel"/></returns>
		TModel ToApi();
	}
}