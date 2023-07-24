namespace Tgstation.Server.Host.Service.Wix.Extensions
{
	using System;
	using System.ServiceProcess;

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

				// As much as I'd like to use Tgstation.Server.Common.Constants.CanonicalPackageName here, attempting to reference it make Tgstation.Server.Migrator.Comms fail due to referencing the net2.0 version of that library. EVEN THOUGH IT'S A TRANSITIVE DEPENDENCY OF Tgstation.Server.Client!!!!!
				// If that dead-ass tool has been removed, feel free to do this
				const string CanonicalPackageName = "tgstation-server";
				session.Log($"Searching for {CanonicalPackageName} service...");
				try
				{
					foreach (var controller in ServiceController.GetServices())
						if (controller.ServiceName == CanonicalPackageName)
							serviceController = controller;
						else
							controller.Dispose();

					if (serviceController == null || serviceController.Status != ServiceControllerStatus.Running)
					{
						session.Log($"{CanonicalPackageName} service not found. Continuing.");
						return ActionResult.Success;
					}

					var commandId = PipeCommands.GetServiceCommandId(
						PipeCommands.CommandDetachingShutdown)
						.Value;

					session.Log($"{CanonicalPackageName} service found. Sending command \"{PipeCommands.CommandDetachingShutdown}\" ({commandId})...");

					serviceController.ExecuteCommand(commandId);

					session.Log($"Command sent. Waiting for {CanonicalPackageName} service to stop...");

					serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));

					var stopped = serviceController.Status == ServiceControllerStatus.Stopped;
					session.Log($"{CanonicalPackageName} stopped {(stopped ? String.Empty : "un")}successfully.");

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
	}
}
