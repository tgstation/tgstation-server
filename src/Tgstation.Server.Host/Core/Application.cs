using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Core
{
    /// <summary>
    /// Configures the ASP.NET Core web application
    /// </summary>
	sealed class Application
	{
		/// <summary>
		/// The version of the <see cref="Application"/>
		/// </summary>
		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// The <see cref="Microsoft.AspNetCore.Hosting.IHostingEnvironment"/> for the <see cref="Application"/>
		/// </summary>
		readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/></param>
		public Application(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
		}

		/// <summary>
		/// Configure dependency injected services
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure</param>
#pragma warning disable CA1822 // Mark members as static
		public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 // Mark members as static
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			var workingDir = Environment.CurrentDirectory;
			var databaseConfigurationSection = configuration.GetSection(DatabaseConfiguration.Section);
			services.Configure<DatabaseConfiguration>(databaseConfigurationSection);

			services.AddOptions();
			
			const string scheme = "JwtBearer";
			services.AddAuthentication((options) =>
			{
				options.DefaultAuthenticateScheme = scheme;
				options.DefaultChallengeScheme = scheme;
			}).AddJwtBearer(scheme, jwtBearerOptions =>
			{
				jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(TokenFactory.TokenSigningKey),

					ValidateIssuer = true,
					ValidIssuer = TokenFactory.TokenIssuer,

					ValidateLifetime = true,
					ValidateAudience = true,
					ValidAudience = TokenFactory.TokenAudience,

					ClockSkew = TimeSpan.FromMinutes(5),

					RequireSignedTokens = true,

					RequireExpirationTime = true
				};
				jwtBearerOptions.Events = new JwtBearerEvents
				{
					OnTokenValidated = ApiController.OnTokenValidated
				};
			});
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); //fucking converts 'sub' to M$ bs

			services.AddMvc();

			var databaseConfiguration = databaseConfigurationSection.Get<DatabaseConfiguration>();
			void ConfigureDatabase(DbContextOptionsBuilder builder)
			{
				if (hostingEnvironment.IsDevelopment())
					builder.EnableSensitiveDataLogging();
			};

			switch (databaseConfiguration.DatabaseType)
			{
				case DatabaseType.MySql:
					services.AddDbContext<MySqlDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<MySqlDatabaseContext>());
					break;
				case DatabaseType.Sqlite:
					services.AddDbContext<SqliteDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<SqliteDatabaseContext>());
					break;
				case DatabaseType.SqlServer:
					services.AddDbContext<SqlServerDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<SqlServerDatabaseContext>());
					break;
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid {0}!", nameof(DatabaseType)));
			}

			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();

			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();
			services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<ISystemIdentityFactory, SystemIdentityFactory>();

			services.AddSingleton<JobManager>();
			services.AddSingleton<IJobManager>(x => x.GetRequiredService<JobManager>());
			services.AddSingleton<IHostedService>(x => x.GetRequiredService<JobManager>());
		}

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public void Configure(IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));

			if (hostingEnvironment.IsDevelopment())
				applicationBuilder.UseDeveloperExceptionPage();
			
			applicationBuilder.UseAuthentication();
			applicationBuilder.UseMvc();
		}
	}
}