using System;
using System.Diagnostics;
using System.IO;

namespace TGServerService
{
	// Some useful functions for triggering pre action events
	partial class TGStationServer
	{
		const string EventFolder = "EventHandlers/";

		string GetPath(string eventName)
		{
			return string.Format("{}{}.bat", EventFolder, eventName);
		}

		bool EventHandlerExists(string eventName)
		{
			return File.Exists(GetPath(eventName));
		}

		bool HandleEvent(string eventName)
		{
			if (!EventHandlerExists(eventName))
			{
				// We don't need a handler, so let's just fail semi-silently.
				TGServerService.WriteWarning(String.Format("Preaction Event not running due to missing event handler: {}.", GetPath(eventName)), TGServerService.EventID.PreactionFail);
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

			TGServerService.WriteInfo(
				String.Format("Preaction Event ran. Stdout:\n{}\nStderr:\n{}", stdout, stderr),
				process.ExitCode == 0 ? TGServerService.EventID.PreactionEvent : TGServerService.EventID.PreactionFail
		   );

			return process.ExitCode == 0 ? true : false;
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
