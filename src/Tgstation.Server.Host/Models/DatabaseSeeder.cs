using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class DatabaseSeeder : IDatabaseSeeder
	{
		/// <summary>
		/// The default password mode admin password
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

		/// <inheritdoc />
		public async Task SeedDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = new User
			{
				AdministrationRights = (AdministrationRights)~0,
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = (InstanceManagerRights)~0,
				Name = "Admin",
				Enabled = true,
			};
			cryptographySuite.RegenerateUserToken(admin);
			cryptographySuite.SetUserPassword(admin, DefaultAdminPassword);
			databaseContext.Users.Add(admin);

			var serverSettings = await databaseContext.GetServerSettings(cancellationToken).ConfigureAwait(false);

			serverSettings.EnableTelemetry = true;
			serverSettings.UpstreamRepository = DefaultUpstreamRepository;

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}
	}
}
