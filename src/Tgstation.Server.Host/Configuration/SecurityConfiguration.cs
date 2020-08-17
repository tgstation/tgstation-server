namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options pertaining to user security
	/// </summary>
	sealed class SecurityConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="SecurityConfiguration"/> resides in
		/// </summary>
		public const string Section = "Security";

		/// <summary>
		/// Default value of <see cref="TokenExpiryMinutes"/>.
		/// </summary>
		const uint DefaultTokenExpiryMinutes = 15;

		/// <summary>
		/// Default value of <see cref="TokenClockSkewMinutes"/>.
		/// </summary>
		const uint DefaultTokenClockSkewMinutes = 1;

		/// <summary>
		/// Default value of <see cref="TokenSigningKeyByteCount"/>.
		/// </summary>
		const uint DefaultTokenSigningKeyByteAmount = 256;

		/// <summary>
		/// Amount of minutes until generated <see cref="Api.Models.Token"/>s expire.
		/// </summary>
		public uint TokenExpiryMinutes { get; set; } = DefaultTokenExpiryMinutes;

		/// <summary>
		/// Amount of minutes to skew the clock for <see cref="Api.Models.Token"/> validation.
		/// </summary>
		public uint TokenClockSkewMinutes { get; set; } = DefaultTokenClockSkewMinutes;

		/// <summary>
		/// Amount of bytes to use in the <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.IssuerSigningKey"/>.
		/// </summary>
		public uint TokenSigningKeyByteCount { get; set; } = DefaultTokenSigningKeyByteAmount;

		/// <summary>
		/// A custom token signing key. Overrides <see cref="TokenSigningKeyByteCount"/>.
		/// </summary>
		public string CustomTokenSigningKeyBase64 { get; set; }
	}
}
