using System;

using Tgstation.Server.Host.Configuration;

#nullable disable

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="GeneralConfiguration"/> <see langword="class"/>.
	/// </summary>
	static class GeneralConfigurationExtensions
	{
		/// <summary>
		/// Gets the total number of tasks that may run simultaneously during an asynchronous directory copy operation.
		/// </summary>
		/// <param name="generalConfiguration">The <see cref="GeneralConfiguration"/> to read the <see cref="GeneralConfiguration.DeploymentDirectoryCopyTasksPerCore"/> from.</param>
		/// <returns>The total number of tasks that may run simultaneously during an asynchronous directory copy operation.</returns>
		public static int? GetCopyDirectoryTaskThrottle(this GeneralConfiguration generalConfiguration)
		{
			ArgumentNullException.ThrowIfNull(generalConfiguration);

			var tasksPerCore = generalConfiguration.DeploymentDirectoryCopyTasksPerCore;
			if (!tasksPerCore.HasValue)
				return null;

			var taskThrottle = (uint)Environment.ProcessorCount * tasksPerCore.Value;
			return (int)taskThrottle;
		}
	}
}
