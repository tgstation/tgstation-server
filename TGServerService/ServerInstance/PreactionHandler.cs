using System;
using System.Diagnostics;
using System.IO;

namespace TGServerService
{
	// Some useful functions for triggering pre action events
	sealed partial class ServerInstance
	{
		const string EventFolder = "EventHandlers/";

		string GetPath(string eventName)
		{
			return string.Format("{0}{1}.bat", EventFolder, eventName);
		}

		bool EventHandlerExists(string eventName)
		{
			return File.Exists(GetPath(eventName));
		}

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
					FileName = GetPath(eventName),
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
			var eventData = String.Format("Preaction Event: {0} @ {1} ran. Stdout:\n{2}\nStderr:\n{3}", eventName, GetPath(eventName), stdout, stderr);

			if (success)
				Service.WriteInfo(eventData, EventID.PreactionEvent);
			else
				Service.WriteWarning(eventData, EventID.PreactionFail);

			return success;
		}

		public bool PrecompileHook()
		{
			return HandleEvent("precompile");
		}

		public bool PostcompileHook()
		{
			return HandleEvent("postcompile");
		}
	}
}
