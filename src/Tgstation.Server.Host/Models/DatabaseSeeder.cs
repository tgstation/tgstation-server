using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class DatabaseSeeder : IDatabaseSeeder
	{
		/// <summary>
		/// The name of the default admin user
		/// </summary>
		const string AdminName = "Admin";

		/// <summary>
		/// The default admin password
		/// </summary>
		const string DefaultAdminPassword = "ISolemlySwearToDeleteTheDataDirectory";

		/// <summary>
		/// The default git repository to pull server updates from
		/// </summary>
		const string DefaultUpstreamRepository = "https://github.com/tgstation/tgstation-server";

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// Construct a <see cref="DatabaseSeeder"/>
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		public DatabaseSeeder(ICryptographySuite cryptographySuite) => this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to</param>
		void SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				AdministrationRights = (AdministrationRights)~0,
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = (InstanceManagerRights)~0,
				Name = AdminName,
				CanonicalName = AdminName.ToUpperInvariant(),
				Enabled = true,
			};
			cryptographySuite.SetUserPassword(admin, DefaultAdminPassword);
			databaseContext.Users.Add(admin);
		}

		/// <inheritdoc />
		public async Task SeedDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			SeedAdminUser(databaseContext);
			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ResetAdminPassword(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await databaseContext.Users.Where(x => x.CanonicalName == AdminName.ToUpperInvariant()).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (admin == default)
				SeedAdminUser(databaseContext);
			else
			{
				admin.Enabled = true;
				cryptographySuite.SetUserPassword(admin, DefaultAdminPassword);
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}
	}
}
