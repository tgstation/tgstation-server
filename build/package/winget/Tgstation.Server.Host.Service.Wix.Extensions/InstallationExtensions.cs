namespace Tgstation.Server.Host.Service.Wix.Extensions
{
	using System;
	using System.IO;
	using System.ServiceProcess;

	using Tgstation.Server.Common;
	using Tgstation.Server.Host.Common;

	using WixToolset.Dtf.WindowsInstaller;

	/// <summary>
	/// Extension methods for the .msi installer.
	/// </summary>
	public static class InstallationExtensions
	{
		/// <summary>
		/// Attempts to detach stop the existing tgstation-server service if it exists.
		/// </summary>
		/// <param name="session">The installer <see cref="Session"/>.</param>
		/// <returns>The <see cref="ActionResult"/> of the custom action.</returns>
		[CustomAction]
		public static ActionResult DetachStopTgsServiceIfRunning(Session session)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));

			try
			{
				session.Log("Begin DetachStopTgsServiceIfRunning");
				ServiceController serviceController = null;

				session.Log($"Searching for {Constants.CanonicalPackageName} service...");
				try
				{
					foreach (var controller in ServiceController.GetServices())
						if (controller.ServiceName == Constants.CanonicalPackageName)
							serviceController = controller;
						else
							controller.Dispose();

					if (serviceController == null || serviceController.Status != ServiceControllerStatus.Running)
					{
						session.Log($"{Constants.CanonicalPackageName} service not found. Continuing.");
						return ActionResult.Success;
					}

					var commandId = PipeCommands.GetServiceCommandId(
						PipeCommands.CommandDetachingShutdown)
						.Value;

					session.Log($"{Constants.CanonicalPackageName} service found. Sending command \"{PipeCommands.CommandDetachingShutdown}\" ({commandId})...");

					serviceController.ExecuteCommand(commandId);

					session.Log($"Command sent. Waiting for {Constants.CanonicalPackageName} service to stop...");

					serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));

					var stopped = serviceController.Status == ServiceControllerStatus.Stopped;
					session.Log($"{Constants.CanonicalPackageName} stopped {(stopped ? String.Empty : "un")}successfully.");

					return stopped
						? ActionResult.Success
						: ActionResult.NotExecuted;
				}
				finally
				{
					serviceController?.Dispose();
				}
			}
			catch (Exception ex)
			{
				session.Log($"Exception in DetachStopTgsServiceIfRunning:{Environment.NewLine}{ex}");
				return ActionResult.Failure;
			}
		}

		/// <summary>
		/// Attempts to copy the initial config to the production config if necessary.
		/// </summary>
		/// <param name="session">The installer <see cref="Session"/>.</param>
		/// <returns>The <see cref="ActionResult"/> of the custom action.</returns>
		[CustomAction]
		public static ActionResult ApplyProductionAppsettingsIfNonExistant(Session session)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));

			try
			{
				session.Log("Begin ApplyProductionAppsettingsIfNonExistant");
				var programDataDirectory = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					Constants.CanonicalPackageName);
				var initialAppSettingsPath = Path.Combine(programDataDirectory, "appsettings.Initial.yml");
				var productionAppSettingsPath = Path.Combine(programDataDirectory, "appsettings.Production.yml");

				var initialAppSettingsExists = File.Exists(initialAppSettingsPath);
				var productionAppSettingsExists = File.Exists(productionAppSettingsPath);

				if (productionAppSettingsExists)
					session.Log("appsettings.Production.yml present");
				else
					session.Log("appsettings.Production.yml NOT present");

				if (!initialAppSettingsExists)
					session.Log("appsettings.Initial.yml NOT present!");
				else
				{
					session.Log("appsettings.Initial.yml present");
					if (!productionAppSettingsExists)
					{
						session.Log("Copying initial settings to production settings...");
						File.Copy(initialAppSettingsPath, productionAppSettingsPath);
						return ActionResult.Success;
					}
				}

				return ActionResult.NotExecuted;
			}
			catch (Exception ex)
			{
				session.Log($"Exception in ApplyProductionAppsettingsIfNonExistant:{Environment.NewLine}{ex}");
				return ActionResult.Failure;
			}
		}
	}
}
