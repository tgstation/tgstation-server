namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Base <see langword="class"/> for tgstation forum responses.
	/// </summary>
	abstract class TGBaseResponse
	{
		/// <summary>
		/// Expected value of <see cref="Status"/>.
		/// </summary>
		public const string OkStatus = "OK";

		/// <summary>
		/// The response status.
		/// </summary>
		public string Status { get; set; }

		/// <summary>
		/// The response error, if any.
		/// </summary>
		public string Error { get; set; }
	}
}
