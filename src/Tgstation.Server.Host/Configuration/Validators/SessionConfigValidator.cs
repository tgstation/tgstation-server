using System;

using Microsoft.Extensions.Options;

namespace Tgstation.Server.Host.Configuration.Validators
{
	/// <summary>
	/// Configuration validator for <see cref="SessionConfiguration"/>.
	/// </summary>
	sealed class SessionConfigValidator : IValidateOptions<SessionConfiguration>
	{
		/// <inheritdoc />
		public ValidateOptionsResult Validate(string? name, SessionConfiguration options)
		{
			ArgumentNullException.ThrowIfNull(options);

			if (options.BridgePort == 0)
				return ValidateOptionsResult.Fail($"{nameof(SessionConfiguration.BridgePort)} cannot be zero!");

			return ValidateOptionsResult.Success;
		}
	}
}
