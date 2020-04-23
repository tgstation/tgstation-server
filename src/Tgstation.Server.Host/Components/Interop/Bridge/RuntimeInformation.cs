using System;
using System.Collections.Generic;
using System.Linq;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Representation of the initial data passed as part of a <see cref="BridgeCommandType.Startup"/> request.
	/// </summary>
	public sealed class RuntimeInformation : ChatChannelsUpdate
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
		public DreamDaemonSecurity? SecurityLevel { get; }

		/// <summary>
		/// The <see cref="TestMergeInformation"/>s in the launch.
		/// </summary>
		public IReadOnlyCollection<TestMergeInformation> TestMerges { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RuntimeInformation"/> <see langword="class"/>.
		/// </summary>
		/// <param name="application">The <see cref="IApplication"/> to use.</param>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> used to generate the value of <see cref="DMApiParameters.AccessIdentifier"/>.</param>
		/// <param name="serverPortProvider">The <see cref="IServerPortProvider"/> used to set the value of <see cref="ServerPort"/>.</param>
		/// <param name="testMerges">An <see cref="IEnumerable{T}"/> used to construct the value of <see cref="TestMerges"/>.</param>
		/// <param name="chatChannels">The <see cref="Chat.ChannelRepresentation"/>s for the <see cref="ChatChannelsUpdate"/>.</param>
		/// <param name="instance">The <see cref="Instance"/> used to set <see cref="InstanceName"/>.</param>
		/// <param name="revision">The value of <see cref="RevisionInformation"/>.</param>
		/// <param name="securityLevel">The value of <see cref="SecurityLevel"/>.</param>
		public RuntimeInformation(
			IApplication application,
			ICryptographySuite cryptographySuite,
			IServerPortProvider serverPortProvider,
			IEnumerable<TestMergeInformation> testMerges,
			IEnumerable<Chat.ChannelRepresentation> chatChannels,
			Api.Models.Instance instance,
			Api.Models.Internal.RevisionInformation revision,
			DreamDaemonSecurity? securityLevel)
			: base(chatChannels)
		{
			ServerVersion = application?.Version ?? throw new ArgumentNullException(nameof(application));
			AccessIdentifier = cryptographySuite?.GetSecureString() ?? throw new ArgumentNullException(nameof(cryptographySuite));
			ServerPort = serverPortProvider?.HttpApiPort ?? throw new ArgumentNullException(nameof(serverPortProvider));
			TestMerges = testMerges?.ToList() ?? throw new ArgumentNullException(nameof(testMerges));
			InstanceName = instance?.Name ?? throw new ArgumentNullException(nameof(instance));
			Revision = revision ?? throw new ArgumentNullException(nameof(revision));
			SecurityLevel = securityLevel;
		}
	}
}
