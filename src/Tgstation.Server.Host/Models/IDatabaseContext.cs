using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Models
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
		/// The <see cref="Models.ChatSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<ChatSettings> ChatSettings { get; set; }

		/// <summary>
		/// The <see cref="ChatChannel"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<ChatChannel> ChatChannels { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Models.ReattachInformation"/>s
		/// </summary>
		DbSet<ReattachInformation> ReattachInformations { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Models.WatchdogReattachInformation"/>s
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
	}
}
