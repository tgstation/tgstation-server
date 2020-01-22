using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// Represents the database
	/// </summary>
	public interface IDatabaseContext
	{
		/// <summary>
		/// The <see cref="User"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<User> Users { get; }

		/// <summary>
		/// The <see cref="Instance"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<Instance> Instances { get; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<InstanceUser> InstanceUsers { get; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<Job> Jobs { get; }

		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<CompileJob> CompileJobs { get; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<RevisionInformation> RevisionInformations { get; }

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<DreamMakerSettings> DreamMakerSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<DreamDaemonSettings> DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<ChatBot> ChatBots { get; set; }

		/// <summary>
		/// The <see cref="ChatChannel"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<ChatChannel> ChatChannels { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="ReattachInformation"/>s
		/// </summary>
		DbSet<ReattachInformation> ReattachInformations { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="WatchdogReattachInformation"/>s
		/// </summary>
		DbSet<WatchdogReattachInformation> WatchdogReattachInformations { get; set; }

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
