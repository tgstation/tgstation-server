using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseSeeder : IDatabaseSeeder
	{
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// Construct a <see cref="DatabaseSeeder"/>
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		public DatabaseSeeder(ICryptographySuite cryptographySuite)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
		}

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to</param>
		void SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				AdministrationRights = ~AdministrationRights.None,
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = ~InstanceManagerRights.None,
				Name = Api.Models.User.AdminName,
				CanonicalName = Api.Models.User.AdminName.ToUpperInvariant(),
				Enabled = true,
			};
			cryptographySuite.SetUserPassword(admin, Api.Models.User.DefaultAdminPassword, true);
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
			var admin = await databaseContext.Users.Where(x => x.CanonicalName == Api.Models.User.AdminName.ToUpperInvariant()).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (admin == default)
				SeedAdminUser(databaseContext);
			else
			{
				admin.Enabled = true;
				cryptographySuite.SetUserPassword(admin, Api.Models.User.DefaultAdminPassword, false);
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}
	}
}
