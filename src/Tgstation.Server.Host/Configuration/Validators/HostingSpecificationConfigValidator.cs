using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Configuration validation for <see cref="HostingSpecification"/>s.
	/// </summary>
	static class HostingSpecificationConfigValidator
	{
		/// <summary>
		/// Validate given a set of <paramref name="endPoints"/>.
		/// </summary>
		/// <param name="endPoints">The <see cref="HostingSpecification"/>s to validate.</param>
		/// <param name="name">The name of the config subsection.</param>
		/// <param name="required">If these endpoints are required.</param>
		/// <returns>The <see cref="ValidateOptionsResult"/> for the <paramref name="endPoints"/>.</returns>
		public static ValidateOptionsResult ValidateHostingSpecifications(IReadOnlyList<HostingSpecification> endPoints, string name, bool required)
		{
			ArgumentNullException.ThrowIfNull(endPoints);
			ArgumentNullException.ThrowIfNull(name);

			if (endPoints == null || endPoints.Count == 0)
			{
				if (!required)
					return ValidateOptionsResult.Success;

				return ValidateOptionsResult.Fail($"At least one hosting specification for {name} must be specified");
			}

			foreach (var endPoint in endPoints)
			{
				if (endPoint == null)
					return ValidateOptionsResult.Fail("A hosting specification cannot be null!");

				if (endPoint.Port == 0)
					return ValidateOptionsResult.Fail($"Hosting specification port in {name} cannot be 0!");

				if (endPoint.IPAddress != null && !IPAddress.TryParse(endPoint.IPAddress, out _))
					return ValidateOptionsResult.Fail($"Could not part hosting IP address in {name}: {endPoint.IPAddress}");
			}

			return ValidateOptionsResult.Success;
		}
	}
}
