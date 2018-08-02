namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an error message returned by the server
	/// </summary>
	public sealed class ErrorMessage
	{
		/// <summary>
		/// A human readable description of the error
		/// </summary>
		public string Message { get; set; }
	}
}
