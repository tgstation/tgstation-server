using Cyberboss.AspNetCore.AsyncInitializer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Core
{
    /// <summary>
    /// Configures the ASP.NET Core web application
    /// </summary>
	sealed class Application
	{
		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		public Application(IConfiguration configuration) => this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

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
			services.Configure<DatabaseConfiguration>(configuration.GetSection("Database"));

            services.AddMvc();
            services.AddOptions();
            services.AddLocalization();

			services.AddDbContext<DatabaseContext>();
			services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<DatabaseContext>());

			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();
			services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
		}

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/> of the <see cref="Application"/></param>
		public void Configure(IApplicationBuilder applicationBuilder, IHostingEnvironment hostingEnvironment)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (hostingEnvironment == null)
				throw new ArgumentNullException(nameof(hostingEnvironment));

			if (hostingEnvironment.IsDevelopment())
				applicationBuilder.UseDeveloperExceptionPage();

			var defaultCulture = new CultureInfo("en");
			var supportedCultures = new List<CultureInfo>
			{
				defaultCulture
			};

			CultureInfo.CurrentCulture = defaultCulture;
			CultureInfo.CurrentUICulture = defaultCulture;

			applicationBuilder.UseRequestLocalization(new RequestLocalizationOptions
			{
				SupportedCultures = supportedCultures,
				SupportedUICultures = supportedCultures,
			});

			applicationBuilder.UseAsyncInitialization(async (cancellationToken) =>
			{
				using (var scop = applicationBuilder.ApplicationServices.CreateScope())
					await scop.ServiceProvider.GetRequiredService<IDatabaseContext>().Initialize(cancellationToken).ConfigureAwait(false);
			});

			applicationBuilder.UseSystemAuthentication();

			applicationBuilder.UseMvc();
		}
	}
}