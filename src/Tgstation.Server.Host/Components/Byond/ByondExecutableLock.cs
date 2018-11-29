using System;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondExecutableLock : IByondExecutableLock
	{
		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public string DreamDaemonPath { get; }

		/// <inheritdoc />
		public string DreamMakerPath { get; }

		/// <summary>
		/// Construct a <see cref="ByondExecutableLock"/>
		/// </summary>
		/// <param name="version">The value of <see cref="Version"/></param>
		/// <param name="dreamDaemonPath">The value of <see cref="DreamDaemonPath"/></param>
		/// <param name="dreamMakerPath">The value of <see cref="DreamMakerPath"/></param>
		public ByondExecutableLock(Version version, string dreamDaemonPath, string dreamMakerPath)
		{
			Version = version ?? throw new ArgumentNullException(nameof(version));
			DreamDaemonPath = dreamDaemonPath ?? throw new ArgumentNullException(nameof(dreamDaemonPath));
			DreamMakerPath = dreamMakerPath ?? throw new ArgumentNullException(nameof(dreamMakerPath));
		}

		// at one point in design, byond versions were to delete themselves if they weren't the active version
		// That changed at some point so these functions are intentioanlly left blank

		/// <inheritdoc />
		public void Dispose() { }

		/// <inheritdoc />
		public void DoNotDeleteThisSession() { }
	}
}
