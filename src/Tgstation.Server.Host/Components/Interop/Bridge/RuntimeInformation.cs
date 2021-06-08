using System;
using System.Collections.Generic;
using System.Linq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Representation of the initial data passed as part of a <see cref="BridgeCommandType.Startup"/> request.
	/// </summary>
	public sealed class RuntimeInformation : ChatUpdate
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider.Version"/>.
		/// </summary>
		public Version ServerVersion { get; }

		/// <summary>
		/// The port the HTTP server is running on.
		/// </summary>
		public int ServerPort { get; }

		/// <summary>
		/// If DD should just respond if it's API is working and then exit.
		/// </summary>
		public bool ApiValidateOnly { get; }

		/// <summary>
		/// The <see cref="NamedEntity.Name"/> of the owner at the time of launch.
		/// </summary>
		public string InstanceName { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Internal.RevisionInformation"/> of the launch.
		/// </summary>
		public Api.Models.Internal.RevisionInformation Revision { get; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of the launch.
		/// </summary>
		public DreamDaemonSecurity? SecurityLevel { get; }

		/// <summary>
		/// The <see cref="TestMergeInformation"/>s in the launch.
		/// </summary>
		public IReadOnlyCollection<TestMergeInformation> TestMerges { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RuntimeInformation"/> class.
		/// </summary>
		/// <param name="chatTrackingContext">The <see cref="IChatTrackingContext"/> to use.</param>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> to get revision information from.</param>
		/// <param name="serverVersion">The value of <see cref="ServerVersion"/>.</param>
		/// <param name="instanceName">The value of <see cref="InstanceName"/>.</param>
		/// <param name="securityLevel">The value of <see cref="SecurityLevel"/>.</param>
		/// <param name="serverPort">The value of <see cref="ServerPort"/>.</param>
		/// <param name="apiValidateOnly">The value of <see cref="ApiValidateOnly"/>.</param>
		public RuntimeInformation(
			IChatTrackingContext chatTrackingContext,
			IDmbProvider dmbProvider,
			Version serverVersion,
			string instanceName,
			DreamDaemonSecurity? securityLevel,
			int serverPort,
			bool apiValidateOnly)
			: base(chatTrackingContext?.Channels ?? throw new ArgumentNullException(nameof(chatTrackingContext)))
		{
			if (dmbProvider == null)
				throw new ArgumentNullException(nameof(dmbProvider));

			ServerVersion = serverVersion ?? throw new ArgumentNullException(nameof(serverVersion));

			Revision = new Api.Models.Internal.RevisionInformation
			{
				CommitSha = dmbProvider.CompileJob.RevisionInformation.CommitSha,
				Timestamp = dmbProvider.CompileJob.RevisionInformation.Timestamp,
				OriginCommitSha = dmbProvider.CompileJob.RevisionInformation.OriginCommitSha,
			};

			TestMerges = (IReadOnlyCollection<TestMergeInformation>)dmbProvider
				.CompileJob
				.RevisionInformation
				.ActiveTestMerges?
				.Select(x => x.TestMerge)
				.Select(x => new TestMergeInformation(x, Revision))
				.ToList()
				?? Array.Empty<TestMergeInformation>();

			InstanceName = instanceName ?? throw new ArgumentNullException(nameof(instanceName));
			SecurityLevel = securityLevel;
			ServerPort = serverPort;
			ApiValidateOnly = apiValidateOnly;
		}
	}
}
