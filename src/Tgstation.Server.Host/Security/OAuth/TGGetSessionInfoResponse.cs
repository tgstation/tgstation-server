namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Response when getting tgstation forum user's info.
	/// </summary>
	sealed class TGGetSessionInfoResponse : TGBaseResponse
	{
		/// <summary>
		/// The user's forum account name.
		/// </summary>
		public string? PhpbbUsername { get; set; }
	}
}
