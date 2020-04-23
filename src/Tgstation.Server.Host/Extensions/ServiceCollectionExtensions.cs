using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Globalization;
using System.Linq;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IServiceCollection"/>
	/// </summary>
	static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Add a standard <typeparamref name="TConfig"/> binding
		/// </summary>
		/// <typeparam name="TConfig">The <see langword="class"/> to bind. Must have a <see langword="public"/> const/static <see cref="string"/> field named "Section"</typeparam>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> containing the <typeparamref name="TConfig"/></param>
		/// <returns><paramref name="serviceCollection"/></returns>
		public static IServiceCollection UseStandardConfig<TConfig>(this IServiceCollection serviceCollection, IConfiguration configuration) where TConfig : class
		{
			if (serviceCollection == null)
				throw new ArgumentNullException(nameof(serviceCollection));
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			const string SectionFieldName = nameof(GeneralConfiguration.Section);

			var configType = typeof(TConfig);
			var sectionField = configType.GetField(SectionFieldName);
			if (sectionField == null)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} has no {1} field!", configType, SectionFieldName));

			var stringType = typeof(string);
			if (sectionField.FieldType != stringType)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} has invalid {1} field type, must be {2}!", configType, SectionFieldName, stringType));

			var sectionName = (string)sectionField.GetValue(null);

			return serviceCollection.Configure<TConfig>(configuration.GetSection(sectionName));
		}

		/// <summary>
		/// Removes the <see cref="EventLogLoggerProvider"/> from a given <paramref name="serviceCollection"/>.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to remove the <see cref="EventLogLoggerProvider"/> from.</param>
		/// <returns>The updated <paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection RemoveEventLogging(this IServiceCollection serviceCollection)
		{
			if (serviceCollection == null)
				throw new ArgumentNullException(nameof(serviceCollection));

			// IMPORTANT: Remove the event log provider, it's shitty and causes issues
			var eventLogDescriptor =
				serviceCollection.FirstOrDefault(
					descriptor => descriptor.ImplementationType == typeof(EventLogLoggerProvider));

			if (eventLogDescriptor != default)
				serviceCollection.Remove(eventLogDescriptor);

			return serviceCollection;
		}
	}
}
