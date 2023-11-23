using System;
using System.Globalization;

#nullable disable

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Represents the result of trying to start a DD process.
	/// </summary>
	public sealed class LaunchResult
	{
		/// <summary>
		/// The time it took for <see cref="global::System.Diagnostics.Process.WaitForInputIdle()"/> to return or the initial bridge request to process. If <see langword="null"/> the startup timed out.
		/// </summary>
		public TimeSpan? StartupTime { get; set; }

		/// <summary>
		/// The <see cref="global::System.Diagnostics.Process.ExitCode"/> if it exited.
		/// </summary>
		public int? ExitCode { get; set; }

		/// <inheritdoc />
		public override string ToString() => String.Format(CultureInfo.InvariantCulture, "Exit Code: {0}, Time {1}ms", ExitCode, StartupTime?.TotalMilliseconds);
	}
}
