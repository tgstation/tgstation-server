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
		DbSet<User> Users { get; }

		DbSet<Instance> Instances { get; }

		Task Save(CancellationToken cancellationToken);

		Task Initialize(CancellationToken cancellationToken);
	}
}
