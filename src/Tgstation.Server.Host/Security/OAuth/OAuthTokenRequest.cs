using System;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Generic OAuth token request.
	/// </summary>
	class OAuthTokenRequest : OAuthConfigurationBase
	{
		/// <summary>
		/// The OAuth code received from the browser.
		/// </summary>
		public string Code { get; }

		/// <summary>
		/// The scopes being requested.
		/// </summary>
		public string Scope { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthTokenRequest"/> <see langword="class"/>.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> to build from.</param>
		/// <param name="code">The value of <see cref="Code"/>.</param>
		/// <param name="scope">The value of <see cref="Scope"/></param>
		public OAuthTokenRequest(OAuthConfigurationBase oAuthConfiguration, string code, string scope)
			: base(oAuthConfiguration)
		{
			Code = code ?? throw new ArgumentNullException(nameof(code));
			Scope = scope ?? throw new ArgumentNullException(nameof(scope));
		}
	}
}
