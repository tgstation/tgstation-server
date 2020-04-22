using System;
using System.Collections.Generic;
using System.Linq;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.Interop.Runtime
{
	/// <summary>
	/// Representation of the initial json passed to DreamDaemon
	/// </summary>
	sealed class RuntimeInformation : RuntimeFileList
	{
		/// <summary>
		/// The code used by the server to authenticate command Topics
		/// </summary>
		public string AccessIdentifier { get; }

		/// <summary>
		/// The <see cref="IApplication.Version"/>.
		/// </summary>
		public Version ServerVersion { get; }

		/// <summary>
		/// The port the HTTP server is running on
		/// </summary>
		public ushort ServerPort { get; }

		/// <summary>
		/// If DD should just respond if it's API is working and then exit.
		/// </summary>
		public bool ApiValidateOnly { get; }

		/// <summary>
		/// The <see cref="Api.Models.Instance.Name"/> of the owner at the time of launch
		/// </summary>
		public string InstanceName { get; }

		/// <summary>
		/// The <see cref="Api.Models.Internal.RevisionInformation"/> of the launch
		/// </summary>
		public Api.Models.Internal.RevisionInformation Revision { get; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of the launch
		/// </summary>
		public DreamDaemonSecurity SecurityLevel { get; }

		/// <summary>
		/// The <see cref="RuntimeTestMerge"/>s in the launch
		/// </summary>
		public IReadOnlyCollection<RuntimeTestMerge> TestMerges { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RuntimeInformation"/> <see langword="class"/>.
		/// </summary>
		/// <param name="application">The <see cref="IApplication"/> to use.</param>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> to use.</param>
		/// <param name="testMerges">An <see cref="IEnumerable{T}"/> used to construct the value of <see cref="TestMerges"/>.</param>
		/// <param name="instance">The <see cref="Instance"/> used to set <see cref="InstanceName"/>.</param>
		/// <param name="revision">The value of <see cref="RevisionInformation"/>.</param>
		/// <param name="channelsJson">The value of <see cref="RuntimeFileList.ChatChannelsJson"/>.</param>
		/// <param name="commandsJson">The value of <see cref="RuntimeFileList.ChatCommandsJson"/>.</param>
		/// <param name="securityLevel">The value of <see cref="SecurityLevel"/>.</param>
		/// <param name="serverPort">The value of <see cref="ServerPort"/>.</param>
		public RuntimeInformation(
			IApplication application,
			ICryptographySuite cryptographySuite,
			IEnumerable<RuntimeTestMerge> testMerges,
			Api.Models.Instance instance,
			Api.Models.Internal.RevisionInformation revision,
			string channelsJson,
			string commandsJson,
			DreamDaemonSecurity securityLevel,
			ushort serverPort)
		{
			ServerVersion = application?.Version ?? throw new ArgumentNullException(nameof(application));
			AccessIdentifier = cryptographySuite?.GetSecureString() ?? throw new ArgumentNullException(nameof(cryptographySuite));
			TestMerges = testMerges?.ToList() ?? throw new ArgumentNullException(nameof(testMerges));
			InstanceName = instance?.Name ?? throw new ArgumentNullException(nameof(instance));
			Revision = revision ?? throw new ArgumentNullException(nameof(revision));
			ChatChannelsJson = channelsJson ?? throw new ArgumentNullException(nameof(channelsJson));
			ChatCommandsJson = commandsJson ?? throw new ArgumentNullException(nameof(commandsJson));
			SecurityLevel = securityLevel;
			ServerPort = serverPort;
		}
	}
}
