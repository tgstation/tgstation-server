using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseSeeder : IDatabaseSeeder
	{
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseSeeder"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="DatabaseSeeder"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// Construct a <see cref="DatabaseSeeder"/>
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		public DatabaseSeeder(ICryptographySuite cryptographySuite, IPlatformIdentifier platformIdentifier)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
		}

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to</param>
		void SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
				Name = Api.Models.User.AdminName,
				CanonicalName = User.CanonicalizeName(Api.Models.User.AdminName),
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
		public async Task SanitizeDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken).ConfigureAwait(false);
			if (admin != null)
			{
				// Fix the issue with ulong enums
				// https://github.com/tgstation/tgstation-server/commit/db341d43b3dab74fe3681f5172ca9bfeaafa6b6d#diff-09f06ec4584665cf89bb77b97f5ccfb9R36-R39
				// https://github.com/JamesNK/Newtonsoft.Json/issues/2301
				admin.AdministrationRights &= RightsHelper.AllRights<AdministrationRights>();
				admin.InstanceManagerRights &= RightsHelper.AllRights<InstanceManagerRights>();
			}

			if (platformIdentifier.IsWindows)
			{
				// normalize backslashes to forward slashes
				var allInstances = await databaseContext
					.Instances
					.AsQueryable()
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);
				foreach (var instance in allInstances)
					instance.Path = instance.Path.Replace('\\', '/');
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ResetAdminPassword(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken).ConfigureAwait(false);
			if (admin != null)
			{
				admin.Enabled = true;
				cryptographySuite.SetUserPassword(admin, Api.Models.User.DefaultAdminPassword, false);
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Get or create the admin <see cref="User"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the admin <see cref="User"/> or <see langword="null"/>. If <see langword="null"/>, <see cref="IDatabaseContext.Save(CancellationToken)"/> must be called on <paramref name="databaseContext"/>.</returns>
		async Task<User> GetAdminUser(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.CanonicalName == User.CanonicalizeName(Api.Models.User.AdminName))
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (admin == default)
				SeedAdminUser(databaseContext);

			return admin;
		}
	}
}
