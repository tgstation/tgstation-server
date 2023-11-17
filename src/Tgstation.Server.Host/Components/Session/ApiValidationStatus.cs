namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Status of DMAPI validation.
	/// </summary>
	enum ApiValidationStatus
	{
		/// <summary>
		/// The DMAPI never contacted the server for validation.
		/// </summary>
		NeverValidated,

		/// <summary>
		/// The server was contacted for validation but it was never requested.
		/// </summary>
		UnaskedValidationRequest,

		/// <summary>
		/// The validation request was malformed.
		/// </summary>
		BadValidationRequest,

		/// <summary>
		/// Valid API. The game must be run with a minimum security level of <see cref="Api.Models.DreamDaemonSecurity.Safe"/>.
		/// </summary>
		RequiresSafe,

		/// <summary>
		/// Valid API. The game must be run with a security level of <see cref="Api.Models.DreamDaemonSecurity.Trusted"/>.
		/// </summary>
		RequiresTrusted,

		/// <summary>
		/// Valid API. The game must be run with a minimum security level of <see cref="Api.Models.DreamDaemonSecurity.Ultrasafe"/>.
		/// </summary>
		RequiresUltrasafe,

		/// <summary>
		/// Valid API, but not compatible with the current TGS version.
		/// </summary>
		Incompatible,
	}
}
