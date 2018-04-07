using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents the database
	/// </summary>
	interface IDatabaseContext
	{
		/// <summary>
		/// The <see cref="ServerSettings"/> in the <see cref="IDatabaseContext"/>
		/// </summary>
		ServerSettings ServerSettings { get; }

		/// <summary>
		/// The <see cref="User"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<User> Users { get; }

		/// <summary>
		/// The <see cref="Instances"/>s in the <see cref="IDatabaseContext"/>
		/// </summary>
		DbSet<Instance> Instances { get; }

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
