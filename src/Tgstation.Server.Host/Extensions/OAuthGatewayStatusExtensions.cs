using System;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="OAuthGatewayStatus"/>.
	/// </summary>
	static class OAuthGatewayStatusExtensions
	{
		/// <summary>
		/// Convert a given <paramref name="oAuthGatewayStatus"/> to a <see cref="Nullable{T}"/> <see cref="bool"/> for API usage.
		/// </summary>
		/// <param name="oAuthGatewayStatus">The <see cref="OAuthGatewayStatus"/> to convert.</param>
		/// <returns>The <see cref="Nullable{T}"/> <see cref="bool"/> form of the <paramref name="oAuthGatewayStatus"/>.</returns>
		public static bool? ToBoolean(this OAuthGatewayStatus oAuthGatewayStatus)
			=> oAuthGatewayStatus switch
			{
				OAuthGatewayStatus.Disabled => null,
				OAuthGatewayStatus.Enabled => false,
				OAuthGatewayStatus.Only => true,
				_ => throw new InvalidOperationException($"Invalid {nameof(OAuthGatewayStatus)}: {oAuthGatewayStatus}"),
			};
	}
}
