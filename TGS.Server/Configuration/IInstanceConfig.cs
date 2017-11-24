using TGS.Interface;
using TGS.Server.IO;

namespace TGS.Server.Configuration
{
	/// <summary>
	/// Configuration settings for a <see cref="Components.Instance"/>
	/// </summary>
	public interface IInstanceConfig
	{
		/// <summary>
		/// The <see cref="Components.Instance"/> directory this <see cref="IInstanceConfig"/> is for
		/// </summary>
		string Directory { get; }

		/// <summary>
		/// Actual version of the <see cref="IInstanceConfig"/>. Migrated up via <see cref="DeprecatedInstanceConfig"/>
		/// </summary>
		ulong Version { get; }

		/// <summary>
		/// The name of the <see cref="Components.Instance"/>
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// If the <see cref="Components.Instance"/> is active
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// The name of the .dme/.dmb the <see cref="Components.Instance"/> uses
		/// </summary>
		string ProjectName { get; set; }

		/// <summary>
		/// The port the <see cref="Components.Instance"/> runs on
		/// </summary>
		ushort Port { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level for the <see cref="Components.Instance"/>
		/// </summary>
		DreamDaemonSecurity Security { get; set; }

		/// <summary>
		/// Whether or not the <see cref="Components.Instance"/> should immediately start DreamDaemon when activated
		/// </summary>
		bool Autostart { get; set; }

		/// <summary>
		/// Whether or not DreamDaemon allows connections from webclients
		/// </summary>
		bool Webclient { get; set; }

		/// <summary>
		/// Author and committer name for synchronize commits
		/// </summary>
		string CommitterName { get; set; }
		/// <summary>
		/// Author and committer e-mail for synchronize commits
		/// </summary>
		string CommitterEmail { get; set; }

		/// <summary>
		/// Encrypted serialized <see cref="ChatSetupInfo"/>s
		/// </summary>
		string ChatProviderData { get; set; }

		/// <summary>
		/// Entropy for <see cref="ChatProviderData"/>
		/// </summary>
		string ChatProviderEntropy { get; set; }

		/// <summary>
		/// If the <see cref="Components.Instance"/> should reattach to a running DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		bool ReattachRequired { get; set; }

		/// <summary>
		/// The <see cref="System.Diagnostics.Process.Id"/> of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		int ReattachProcessID { get; set; }

		/// <summary>
		/// The port the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		ushort ReattachPort { get; set; }

		/// <summary>
		/// The serviceCommsKey the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		string ReattachCommsKey { get; set; }

		/// <summary>
		/// The API version of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		string ReattachAPIVersion { get; set; }

		/// <summary>
		/// The user group allowed to use the <see cref="Components.Instance"/>
		/// </summary>
		string AuthorizedUserGroupSID { get; set; }

		/// <summary>
		/// The auto update interval for the <see cref="Components.Instance"/>
		/// </summary>
		ulong AutoUpdateInterval { get; set; }

		/// <summary>
		/// Whether or not testmerge commits are published to a temporary remote branch
		/// </summary>
		bool PushTestmergeCommits { get; set; }

		/// <summary>
		/// Saves the <see cref="IInstanceConfig"/> to it's <see cref="Components.Instance"/> <see cref="Directory"/>
		/// </summary>
		/// <param name="IO">The <see cref="IIOManager"/> to use</param>
		void Save(IIOManager IO);
	}
}
