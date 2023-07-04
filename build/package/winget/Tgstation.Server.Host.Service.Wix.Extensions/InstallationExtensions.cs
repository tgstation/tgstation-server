namespace Tgstation.Server.Host.Service.Wix.SafeShutdown
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

				session.Log($"Searching for {Constants.CanonicalPackageName} service...");
				foreach (var controller in ServiceController.GetServices())
					if (controller.ServiceName == Constants.CanonicalPackageName)
					{
						serviceController = controller;
						break;
					}
					else
						controller.Dispose();

				using (serviceController)
				{
					if (serviceController == null || serviceController.Status != ServiceControllerStatus.Running)
					{
						session.Log($"{Constants.CanonicalPackageName} service not found. Continuing.");
						return ActionResult.Success;
					}

					var commandId = PipeCommands.GetCommandId(
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
			}
			catch (Exception ex)
			{
				session.Log($"Exception in DetachStopTgsServiceIfRunning:{Environment.NewLine}{ex}");
				return ActionResult.Failure;
			}
		}
	}
}
