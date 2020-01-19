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
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IDefaultLogin"/> for the <see cref="DatabaseSeeder"/>
		/// </summary>
		readonly IDefaultLogin defaultLogin;

		/// <summary>
		/// Construct a <see cref="DatabaseSeeder"/>
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="defaultLogin">The value of <see cref="defaultLogin"/>.</param>
		public DatabaseSeeder(ICryptographySuite cryptographySuite, IDefaultLogin defaultLogin)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.defaultLogin = defaultLogin ?? throw new ArgumentNullException(nameof(defaultLogin));
		}

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to</param>
		void SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				AdministrationRights = (AdministrationRights)~0U,
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = (InstanceManagerRights)~0U,
				Name = defaultLogin.UserName,
				CanonicalName = defaultLogin.UserName.ToUpperInvariant(),
				Enabled = true,
			};
			cryptographySuite.SetUserPassword(admin, defaultLogin.Password, true);
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
			var admin = await databaseContext.Users.Where(x => x.CanonicalName == defaultLogin.UserName.ToUpperInvariant()).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (admin == default)
				SeedAdminUser(databaseContext);
			else
			{
				admin.Enabled = true;
				cryptographySuite.SetUserPassword(admin, defaultLogin.Password, false);
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}
	}
}
