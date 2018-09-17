namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Status of DMAPI validation
	/// </summary>
	enum ApiValidationStatus
	{
		/// <summary>
		/// The DMAPI never contacted the server for validation
		/// </summary>
		NeverValidated,
		/// <summary>
		/// The server was contacted for validation but it was never requested
		/// </summary>
		UnaskedValidationRequest,
		/// <summary>
		/// The game must be run with a minimum security level of <see cref="Api.Models.DreamDaemonSecurity.Safe"/>
		/// </summary>
		RequiresSafe,
		/// <summary>
		/// The game must be run with a security level of <see cref="Api.Models.DreamDaemonSecurity.Trusted"/>
		/// </summary>
		RequiresTrusted,
		/// <summary>
		/// The validation request was malformed
		/// </summary>
		BadValidationRequest,
		/// <summary>
		/// The DMAPI validated successfully
		/// </summary>
		Validated
	}
}
