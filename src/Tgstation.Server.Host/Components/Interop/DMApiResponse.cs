#nullable disable

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Common base for interop responses.
	/// </summary>
	public abstract class DMApiResponse
	{
		/// <summary>
		/// Any errors in the client's parameters.
		/// </summary>
		public string ErrorMessage { get; set; }
	}
}
