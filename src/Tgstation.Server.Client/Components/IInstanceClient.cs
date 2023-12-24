using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing a single <see cref="Instance"/>.
	/// </summary>
	public interface IInstanceClient
	{
		/// <summary>
		/// The <see cref="Instance"/> used to create the <see cref="IInstanceClient"/>.
		/// </summary>
		Instance Metadata { get; }

		/// <summary>
		/// Access the <see cref="IEngineClient"/>.
		/// </summary>
		IEngineClient Engine { get; }

		/// <summary>
		/// Access the <see cref="IRepositoryClient"/>.
		/// </summary>
		IRepositoryClient Repository { get; }

		/// <summary>
		/// Access the <see cref="IDreamDaemonClient"/>.
		/// </summary>
		IDreamDaemonClient DreamDaemon { get; }

		/// <summary>
		/// Access the <see cref="IConfigurationClient"/>.
		/// </summary>
		IConfigurationClient Configuration { get; }

		/// <summary>
		/// Access the <see cref="IInstancePermissionSetClient"/>.
		/// </summary>
		IInstancePermissionSetClient PermissionSets { get; }

		/// <summary>
		/// Access the <see cref="IChatBotsClient"/>.
		/// </summary>
		IChatBotsClient ChatBots { get; }

		/// <summary>
		/// Access the <see cref="IDreamMakerClient"/>.
		/// </summary>
		IDreamMakerClient DreamMaker { get; }

		/// <summary>
		/// Access the <see cref="IJobsClient"/>.
		/// </summary>
		IJobsClient Jobs { get; }
	}
}
