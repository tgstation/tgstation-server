using System;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondExecutableLock : ReferenceCounter<ByondInstallation>, IByondExecutableLock
	{
		/// <inheritdoc />
		public Version Version => Instance.Version;

		/// <inheritdoc />
		public string DreamDaemonPath => Instance.DreamDaemonPath;

		/// <inheritdoc />
		public string DreamMakerPath => Instance.DreamMakerPath;

		/// <inheritdoc />
		public bool SupportsCli => Instance.SupportsCli;

		/// <inheritdoc />
		public bool SupportsMapThreads => Instance.SupportsMapThreads;

		/// <inheritdoc />
		public void DoNotDeleteThisSession() => DangerousDropReference();
	}
}
