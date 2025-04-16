using System;

using Microsoft.Extensions.Options;

namespace Tgstation.Server.Host.Configuration.Validators
{
	/// <summary>
	/// Configuration validator for <see cref="GeneralConfiguration"/>.
	/// </summary>
	sealed class GeneralConfigValidator : IValidateOptions<GeneralConfiguration>
	{
		/// <inheritdoc />
		public ValidateOptionsResult Validate(string? name, GeneralConfiguration options)
		{
			ArgumentNullException.ThrowIfNull(options);

			var metricsValidation = HostingSpecificationConfigValidator.ValidateHostingSpecifications(options.MetricsEndPoints, nameof(options.MetricsEndPoints), false);
			if (metricsValidation.Failed)
				return metricsValidation;

			return HostingSpecificationConfigValidator.ValidateHostingSpecifications(options.ApiEndPoints, nameof(GeneralConfiguration.ApiEndPoints), true);
		}
	}
}
