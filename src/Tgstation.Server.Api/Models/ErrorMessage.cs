using System;

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

		/// <summary>
		/// The version of the API the server is using
		/// </summary>
		public Version SeverApiVersion { get; set; } = ApiHeaders.assemblyName.Version;
	}
}
