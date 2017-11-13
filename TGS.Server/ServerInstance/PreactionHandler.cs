using System;
using System.Diagnostics;
using System.IO;

namespace TGS.Server
{
	// Some useful functions for triggering pre action events
	sealed partial class ServerInstance
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
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		bool HandleEvent(string eventName)
		{

			if (!EventHandlerExists(eventName))
			{
				// We don't need a handler, so let's just fail silently.
				return true;
			}

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = GetEventPath(eventName),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};
			process.Start();
			process.WaitForExit();

			var stdout = process.StandardOutput.ReadToEnd();
			var stderr = process.StandardError.ReadToEnd();
			var success = process.ExitCode == 0;
			var eventData = String.Format("Preaction Event: {0} @ {1} ran. Stdout:\n{2}\nStderr:\n{3}", eventName, GetEventPath(eventName), stdout, stderr);

			if (success)
				WriteInfo(eventData, EventID.PreactionEvent);
			else
				WriteWarning(eventData, EventID.PreactionFail);

			return success;
		}

		/// <summary>
		/// Run the "precompile" event
		/// </summary>
		/// <returns><see langword="false"/> if the event handler exists and failed to run, <see langword="true"/> otherwise</returns>
		public bool PrecompileHook()
		{
			return HandleEvent("precompile");
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
