namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The DMAPI validation setting for deployments.
	/// </summary>
	public enum DMApiValidationMode
	{
		/// <summary>
		/// DMAPI validation is performed but not required for the deployment to succeed.
		/// </summary>
		Optional,

		/// <summary>
		/// DMAPI validation must suceed for the deployment to succeed.
		/// </summary>
		Required,

		/// <summary>
		/// DMAPI validation will not be performed and no DMAPI features will be available in the deployment.
		/// </summary>
		Skipped,
	}
}
