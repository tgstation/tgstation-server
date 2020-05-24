using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an error message returned by the server
	/// </summary>
	public sealed class ErrorMessage
	{
		/// <summary>
		/// The version of the API the server is using
		/// </summary>
		[Required]
		public Version? ServerApiVersion { get; set; }

		/// <summary>
		/// A human readable description of the error
		/// </summary>
		[Required]
		public string? Message { get; set; }

		/// <summary>
		/// Additional data associated with the error message.
		/// </summary>
		public string? AdditionalData { get; set; }

		/// <summary>
		/// The <see cref="ErrorCode"/> of the <see cref="ErrorMessage"/>.
		/// </summary>
		[EnumDataType(typeof(ErrorCode))]
		public ErrorCode ErrorCode { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessage"/> <see langword="class"/>.
		/// </summary>
		public ErrorMessage() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessage"/> <see langword="class"/>.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorMessage"/>.</param>
		public ErrorMessage(ErrorCode errorCode)
		{
			ErrorCode = errorCode;
			Message = errorCode.Describe();
			ServerApiVersion = ApiHeaders.Version;
		}
	}
}
