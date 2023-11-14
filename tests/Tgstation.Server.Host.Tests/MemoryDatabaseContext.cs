using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Host.Tests
{
	sealed class MemoryDatabaseContext : DatabaseContext
	{
		public MemoryDatabaseContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
		{
		}
	}
}
