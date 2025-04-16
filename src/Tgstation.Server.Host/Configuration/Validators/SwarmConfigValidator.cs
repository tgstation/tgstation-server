using System;

using Microsoft.Extensions.Options;

namespace Tgstation.Server.Host.Configuration.Validators
{
	/// <summary>
	/// Configuration validator for <see cref="SwarmConfiguration"/>.
	/// </summary>
	sealed class SwarmConfigValidator : IValidateOptions<SwarmConfiguration>
	{
		/// <inheritdoc />
		public ValidateOptionsResult Validate(string? name, SwarmConfiguration options)
		{
			ArgumentNullException.ThrowIfNull(options);

			if (options.PrivateKey == null)
				return ValidateOptionsResult.Success;

			if (options.UpdateRequiredNodeCount == 0)
				return ValidateOptionsResult.Fail($"{nameof(SwarmConfiguration.UpdateRequiredNodeCount)} must be greater than 0!");

			if (options.Address == null)
				return ValidateOptionsResult.Fail($"{nameof(SwarmConfiguration.Address)} must be set to an http endpoint of this swarm service accessible from other servers in the swarm!");

			return HostingSpecificationConfigValidator.ValidateHostingSpecifications(options.EndPoints, nameof(SwarmConfiguration.EndPoints), true);
		}
	}
}
