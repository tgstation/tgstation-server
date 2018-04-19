using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// For initially seeding a database
	/// </summary>
    interface IDatabaseSeeder
    {
		/// <summary>
		/// Initially seed a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to seed</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SeedDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken);
    }
}
