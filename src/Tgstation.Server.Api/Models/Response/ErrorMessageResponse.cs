using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an error message returned by the server
	/// </summary>
	public sealed class ErrorMessageResponse
	{
		/// <summary>
		/// The version of the API the server is using
		/// </summary>
		public Version? ServerApiVersion { get; set; }

		/// <summary>
		/// A human readable description of the error
		/// </summary>
		public string? Message { get; set; }

		/// <summary>
		/// Additional data associated with the error message.
		/// </summary>
		[ResponseOptions]
		public string? AdditionalData { get; set; }

		/// <summary>
		/// The <see cref="ErrorCode"/> of the <see cref="ErrorMessageResponse"/>.
		/// </summary>
		[EnumDataType(typeof(ErrorCode))]
		public ErrorCode ErrorCode { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessageResponse"/> <see langword="class"/>.
		/// </summary>
		public ErrorMessageResponse() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessageResponse"/> <see langword="class"/>.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorMessageResponse"/>.</param>
		public ErrorMessageResponse(ErrorCode errorCode)
		{
			ErrorCode = errorCode;
			Message = errorCode.Describe();
			ServerApiVersion = ApiHeaders.Version;
		}
	}
}
