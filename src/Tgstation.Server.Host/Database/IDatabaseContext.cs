using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
		/// The <see cref="User"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<User> Users { get; }

		/// <summary>
		/// The <see cref="Instance"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<Instance> Instances { get; }

		/// <summary>
		/// The <see cref="InstancePermissionSet"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<InstancePermissionSet> InstancePermissionSets { get; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<Job> Jobs { get; }

		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<CompileJob> CompileJobs { get; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<RevisionInformation> RevisionInformations { get; }

		/// <summary>
		/// The <see cref="RevInfoTestMerge"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<RevInfoTestMerge> RevInfoTestMerges { get; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<TestMerge> TestMerges { get; }

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<DreamMakerSettings> DreamMakerSettings { get; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<DreamDaemonSettings> DreamDaemonSettings { get; }

		/// <summary>
		/// The <see cref="ChatBot"/>s in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<ChatBot> ChatBots { get; }

		/// <summary>
		/// The <see cref="ChatChannel"/> in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<ChatChannel> ChatChannels { get; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="IDatabaseContext"/>.
		/// </summary>
		IDatabaseCollection<RepositorySettings> RepositorySettings { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="ReattachInformation"/>s.
		/// </summary>
		IDatabaseCollection<ReattachInformation> ReattachInformations { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="OAuthConnection"/>s.
		/// </summary>
		IDatabaseCollection<OAuthConnection> OAuthConnections { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="OidcConnection"/>s.
		/// </summary>
		IDatabaseCollection<OidcConnection> OidcConnections { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="UserGroup"/>s.
		/// </summary>
		IDatabaseCollection<UserGroup> Groups { get; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="PermissionSet"/>s.
		/// </summary>
		IDatabaseCollection<PermissionSet> PermissionSets { get; }

		/// <summary>
		/// Saves changes made to the <see cref="IDatabaseContext"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Save(CancellationToken cancellationToken);

		/// <summary>
		/// Attempts to delete all tables and drop the database in use.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Drop(CancellationToken cancellationToken);

		/// <summary>
		/// Creates and migrates the <see cref="IDatabaseContext"/>.
		/// </summary>
		/// <param name="logger">The <see cref="DatabaseContext"/> <see cref="ILogger"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the database should be seeded, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> Migrate(ILogger<DatabaseContext> logger, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to downgrade the schema to the migration used for a given server <paramref name="targetVersion"/>.
		/// </summary>
		/// <param name="logger">The <see cref="DatabaseContext"/> <see cref="ILogger"/> to use.</param>
		/// <param name="targetVersion">The tgstation-server <see cref="Version"/> that the schema should downgrade for.</param>
		/// <param name="currentDatabaseType">The <see cref="DatabaseType"/> in use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask SchemaDowngradeForServerVersion(
			ILogger<DatabaseContext> logger,
			Version targetVersion,
			DatabaseType currentDatabaseType,
			CancellationToken cancellationToken);
	}
}
