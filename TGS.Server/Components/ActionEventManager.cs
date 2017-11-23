using System;
using System.Diagnostics;
using System.IO;
using TGS.Server.IO;
using TGS.Server.Logging;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class ActionEventManager : IActionEventManager
	{
		/// <summary>
		/// The instance directory for preaction handlers
		/// </summary>
		const string EventFolder = "EventHandlers";

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="ActionEventManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ActionEventManager"/>
		/// </summary>
		readonly IIOManager IO;

		/// <summary>
		/// Construct an <see cref="ActionEventManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		public ActionEventManager(IInstanceLogger logger, IIOManager io)
		{
			Logger = logger;
			IO = io;

			IO.CreateDirectory(EventFolder);
		}

		/// <summary>
		/// Gets the path of an event given an <paramref name="eventName"/>
		/// </summary>
		/// <param name="eventName">The name of the event</param>
		/// <returns>The path to the event handler</returns>
		string GetEventPath(string eventName)
		{
			return Path.Combine(EventFolder, String.Format("{0}.bat", eventName));
		}

		/// <summary>
		/// Check if an <see cref="ActionEvent"/> handler for <paramref name="eventName"/> exists
		/// </summary>
		/// <param name="eventName">One of the <see cref="ActionEvent"/>s</param>
		/// <returns><see langword="true"/> if the <see cref="ActionEvent"/> handler exists, <see langword="false"/> otherwise</returns>
		bool EventHandlerExists(string eventName)
		{
			return IO.FileExists(GetEventPath(eventName));
		}

		/// <inheritdoc />
		public bool HandleEvent(string eventName)
		{
			if (!EventHandlerExists(eventName))
				// We don't need a handler, so let's just fail silently.
				return true;

			string eventData;
			bool success;
			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = GetEventPath(eventName),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			})
			{
				process.Start();
				process.WaitForExit();

				var stdout = process.StandardOutput.ReadToEnd();
				var stderr = process.StandardError.ReadToEnd();
				success = process.ExitCode == 0;
				eventData = String.Format("Action event: {0} @ {1} ran. Stdout:\n{2}\nStderr:\n{3}", eventName, GetEventPath(eventName), stdout, stderr);
			}

			if (success)
				Logger.WriteInfo(eventData, EventID.PreactionEvent);
			else
				Logger.WriteWarning(eventData, EventID.PreactionFail);

			return success;
		}
	}
}
