using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// Represents the database.
	/// </summary>
	public interface IDatabaseContext
	{
		/// <summary>
		/// The <see cref="DatabaseType"/>.
		/// </summary>
		DatabaseType DatabaseType { get; }

		/// <summary>
		/// The <see cref="User"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<User> Users { get; }

		/// <summary>
		/// The <see cref="Instance"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<Instance> Instances { get; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<InstanceUser> InstanceUsers { get; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<Job> Jobs { get; }

		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<CompileJob> CompileJobs { get; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<RevisionInformation> RevisionInformations { get; }

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<DreamMakerSettings> DreamMakerSettings { get; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<DreamDaemonSettings> DreamDaemonSettings { get; }

		/// <summary>
		/// The <see cref="ChatBot"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<ChatBot> ChatBots { get; }

		/// <summary>
		/// The <see cref="ChatChannel"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<ChatChannel> ChatChannels { get; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		IDatabaseCollection<RepositorySettings> RepositorySettings { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="ReattachInformation"/>s
		/// </summary>
		IDatabaseCollection<ReattachInformation> ReattachInformations { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="DualReattachInformation"/>s
		/// </summary>
		IDatabaseCollection<DualReattachInformation> WatchdogReattachInformations { get; }

		/// <summary>
		/// Saves changes made to the <see cref="IDatabaseContext"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Save(CancellationToken cancellationToken);

		/// <summary>
		/// Creates and migrates the <see cref="IDatabaseContext"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Initialize(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to downgrade the schema to the migration used for a given server <paramref name="version"/>
		/// </summary>
		/// <param name="version">The tgstation-server <see cref="Version"/> that the schema should downgrade for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SchemaDowngradeForServerVersion(Version version, CancellationToken cancellationToken);
	}
}
