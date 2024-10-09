namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Response for a <see cref="SwarmRegistrationRequest"/>.
	/// </summary>
	public sealed class SwarmRegistrationResponse
	{
		/// <summary>
		/// The base64 encoded token signing key.
		/// </summary>
		public required string TokenSigningKeyBase64 { get; init; }
	}
}
