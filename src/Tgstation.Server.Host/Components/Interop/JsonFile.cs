using System.Collections.Generic;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Representation of the initial json passed to DreamDaemon
	/// </summary>
	sealed class JsonFile
	{
		/// <summary>
		/// The code used by the server to authenticate command Topics
		/// </summary>
		public string AccessIdentifier { get; set; }

		/// <summary>
		/// If DD should just respond if it's API is working and then exit
		/// </summary>
		public bool ApiValidateOnly { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Instance.Name"/> of the owner at the time of launch
		/// </summary>
		public string InstanceName { get; set; }

		/// <summary>
		/// JSON file name that contains current active chat channel information
		/// </summary>
		public string ChatChannelsJson { get; set; }

		/// <summary>
		/// JSON file DD should write to with available chat commands
		/// </summary>
		public string ChatCommandsJson { get; set; }

		/// <summary>
		/// JSON file DD should write to to send commands to the server
		/// </summary>
		public string ServerCommandsJson { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Internal.RevisionInformation"/> of the launch
		/// </summary>
		public Api.Models.Internal.RevisionInformation Revision { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of the launch
		/// </summary>
		public DreamDaemonSecurity SecurityLevel { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the launch
		/// </summary>
		public List<TestMerge> TestMerges { get; } = new List<TestMerge>();
	}
}
