using System;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Generic OAuth token request.
	/// </summary>
	class OAuthTokenRequest : OAuthConfiguration
	{
		/// <summary>
		/// The OAuth code.
		/// </summary>
		public string Code { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthTokenRequest"/>
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> to build from.</param>
		/// <param name="code">The OAuth code received from the browser.</param>
		public OAuthTokenRequest(OAuthConfiguration oAuthConfiguration, string code)
			: base(oAuthConfiguration)
		{
			Code = code ?? throw new ArgumentNullException(nameof(code));
		}
	}
}
