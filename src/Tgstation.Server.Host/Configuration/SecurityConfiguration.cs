using System;
using System.Collections.Generic;
using System.Linq;

using Swashbuckle.AspNetCore.SwaggerGen;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options pertaining to user security.
	/// </summary>
	public sealed class SecurityConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="SecurityConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Security";

		/// <summary>
		/// Default value of <see cref="TokenExpiryMinutes"/>.
		/// </summary>
		const uint DefaultTokenExpiryMinutes = 15;

		/// <summary>
		/// Default value of <see cref="OAuthTokenExpiryMinutes"/>.
		/// </summary>
		const uint DefaultOAuthTokenExpiryMinutes = 60 * 24; // 1 day

		/// <summary>
		/// Default value of <see cref="TokenClockSkewMinutes"/>.
		/// </summary>
		const uint DefaultTokenClockSkewMinutes = 1;

		/// <summary>
		/// Default value of <see cref="TokenSigningKeyByteCount"/>.
		/// </summary>
		const uint DefaultTokenSigningKeyByteAmount = 256;

		/// <summary>
		/// Amount of minutes until <see cref="Api.Models.Response.TokenResponse"/>s generated from passwords expire.
		/// </summary>
		public uint TokenExpiryMinutes { get; set; } = DefaultTokenExpiryMinutes;

		/// <summary>
		/// Amount of minutes to skew the clock for <see cref="Api.Models.Response.TokenResponse"/> validation.
		/// </summary>
		public uint TokenClockSkewMinutes { get; set; } = DefaultTokenClockSkewMinutes;

		/// <summary>
		/// Amount of bytes to use in the <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.IssuerSigningKey"/>.
		/// </summary>
		public uint TokenSigningKeyByteCount { get; set; } = DefaultTokenSigningKeyByteAmount;

		/// <summary>
		/// Amount of minutes until <see cref="Api.Models.Response.TokenResponse"/>s generated from OAuth logins expire.
		/// </summary>
		public uint OAuthTokenExpiryMinutes { get; set; } = DefaultOAuthTokenExpiryMinutes;

		/// <summary>
		/// A custom token signing key. Overrides <see cref="TokenSigningKeyByteCount"/>.
		/// </summary>
		public string? CustomTokenSigningKeyBase64 { get; set; }

		/// <summary>
		/// OAuth provider settings.
		/// </summary>
		public IDictionary<OAuthProvider, OAuthConfiguration>? OAuth
		{
			get => oAuth;
			set
			{
				// Workaround for https://github.com/dotnet/runtime/issues/89547
				var publicProperties = typeof(OAuthConfiguration)
					.GetProperties()
					.Where(property => property.CanWrite && property.SetMethod!.IsPublic)
					.ToList();
				oAuth = value
					?.Where(
						kvp => !publicProperties.All(
							prop => prop.GetValue(kvp.Value) == prop.PropertyType.GetDefaultValue()))
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			}
		}

		/// <summary>
		/// Backing field for <see cref="OAuth"/>.
		/// </summary>
		IDictionary<OAuthProvider, OAuthConfiguration>? oAuth;

		/// <summary>
		/// OIDC provider settings keyed by scheme name.
		/// </summary>
		public IDictionary<string, OidcConfiguration>? OpenIDConnect { get; set; }

		/// <summary>
		/// Get the <see cref="OidcProviderInfo"/>s from the <see cref="SecurityConfiguration"/>.
		/// </summary>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="OidcProviderInfo"/>s.</returns>
		public IEnumerable<OidcProviderInfo> OidcProviderInfos()
			=> OpenIDConnect?.Select(oidcConfig => new OidcProviderInfo
			{
				SchemeKey = oidcConfig.Key,
				FriendlyName = oidcConfig.Value.FriendlyName ?? oidcConfig.Key,
				ThemeColour = oidcConfig.Value.ThemeColour,
				ThemeIconUrl = oidcConfig.Value.ThemeIconUrl,
			}) ?? Enumerable.Empty<OidcProviderInfo>();
	}
}
