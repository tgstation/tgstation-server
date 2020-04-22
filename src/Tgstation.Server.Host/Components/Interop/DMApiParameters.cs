namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Common base for interop parameters.
	/// </summary>
	public abstract class DMApiParameters
	{
		/// <summary>
		/// The <see cref="Runtime.RuntimeInformation.AccessIdentifier"/> for interop.
		/// </summary>
		public string AccessIdentifier { get; set; }
	}
}
