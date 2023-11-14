using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Host.Tests
{
	static class Utils
	{
		public static MemoryDatabaseContext CreateDatabaseContext()
		{
			var options = new DbContextOptionsBuilder<MemoryDatabaseContext>()
				.UseInMemoryDatabase(databaseName: "TgsTestDB")
				.Options;
			return new MemoryDatabaseContext(options);
		}
	}
}
