namespace Tgstation.Server.Host.Setup
{
	/// <summary>
	/// Determines if the <see cref="SetupWizard"/> will run.
	/// </summary>
	public enum SetupWizardMode
	{
		/// <summary>
		/// Run the wizard if the appsettings.{Environment}.yml is not present or empty.
		/// </summary>
		Autodetect,

		/// <summary>
		/// Force run the wizard.
		/// </summary>
		Force,

		/// <summary>
		/// Only run the wizard and exit.
		/// </summary>
		Only,

		/// <summary>
		/// Never run the wizard.
		/// </summary>
		Never,
	}
}
