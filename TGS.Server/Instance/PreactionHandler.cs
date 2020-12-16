using System;
using System.Diagnostics;
using System.IO;

namespace TGS.Server
{
	// Some useful functions for triggering pre action events
	sealed partial class Instance
	{
		/// <summary>
		/// The instance directory for Preaction handlers
		/// </summary>
		const string EventFolder = "EventHandlers/";

		/// <summary>
		/// Creates the <see cref="EventFolder"/>
		/// </summary>
		void InitEventHandlers()
		{
			Directory.CreateDirectory(RelativePath(EventFolder));
		}

		/// <summary>
		/// Gets the path of an event given an <paramref name="eventName"/>
		/// </summary>
		/// <param name="eventName">The name of the event</param>
		/// <returns>The path to the event handler</returns>
		string GetEventPath(string eventName)
		{
			return string.Format("{0}{1}.bat", RelativePath(EventFolder), eventName);
		}

		/// <summary>
		/// Check if an event handler for <paramref name="eventName"/> exists
		/// </summary>
		/// <param name="eventName">The name of the event</param>
		/// <returns><see langword="true"/> if the event handler exists, <see langword="false"/> otherwise</returns>
		bool EventHandlerExists(string eventName)
		{
			return File.Exists(GetEventPath(eventName));
		}

		/// <summary>
		/// Runs an event named <paramref name="eventName"/> if it exists
		/// </summary>
		/// <param name="eventName">The name of the event</param>
		/// <param name="arguments">The arguments for the event</param>
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		bool HandleEvent(string eventName, string arguments = null)
		{

			if (!EventHandlerExists(eventName))
			{
				// We don't need a handler, so let's just fail silently.
				return true;
			}

			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Path.GetFullPath(GetEventPath(eventName)).Replace('\\', '/'),
					UseShellExecute = false,
					WorkingDirectory = Path.GetFullPath(RelativePath(EventFolder)),
				}
			})
			{
				if (arguments != null)
					process.StartInfo.Arguments = arguments;

				process.Start();
				process.WaitForExit();

				var success = process.ExitCode == 0;
				var eventData = String.Format("Preaction Event: {0} @ {1} ran. Exit Code: {2}", eventName, GetEventPath(eventName), process.ExitCode);

				if (success)
					WriteInfo(eventData, EventID.PreactionEvent);
				else
					WriteWarning(eventData, EventID.PreactionFail);

				return success;
			}
		}

		/// <summary>
		/// Run the "precompile" event
		/// </summary>
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		public bool PrecompileHook(string arguments = null)
		{
			return HandleEvent("precompile", arguments);
		}

		/// <summary>
		/// Run the "postcompile" event
		/// </summary>
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		public bool PostcompileHook()
		{
			return HandleEvent("postcompile");
		}
	}
}
