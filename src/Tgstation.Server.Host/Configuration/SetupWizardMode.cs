namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Determines if the <see cref="Setup.SetupWizard"/> will run
	/// </summary>
	public enum SetupWizardMode
	{
		/// <summary>
		/// Run the wizard if the appsettings.{Environment}.json is not present or empty
		/// </summary>
		Autodetect,

		/// <summary>
		/// Force run the wizard
		/// </summary>
		Force,

		/// <summary>
		/// Only run the wizard and exit
		/// </summary>
		Only,

		/// <summary>
		/// Never run the wizard
		/// </summary>
		Never
	}
}
