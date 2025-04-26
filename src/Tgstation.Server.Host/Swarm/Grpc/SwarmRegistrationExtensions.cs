using System;

using Grpc.Core;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// Extension methods for the <see cref="SwarmRegistration"/> class.
	/// </summary>
	static class SwarmRegistrationExtensions
	{
		/// <summary>
		/// Convert a given <paramref name="registration"/> to it's <see cref="Guid"/> representation.
		/// </summary>
		/// <param name="registration">The <see cref="SwarmRegistration"/> to parse.</param>
		/// <returns>The <see cref="Guid"/> representation of the <paramref name="registration"/>.</returns>
		public static Guid ToGuid(this SwarmRegistration registration)
		{
			if (!Guid.TryParse(registration.Id, out Guid guid))
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, "Swarm registration not in correct format!"));

			return guid;
		}

		/// <summary>
		/// Validates a given <paramref name="registration"/>.
		/// </summary>
		/// <param name="registration">The <see cref="SwarmRegistration"/> to validate.</param>
		/// <param name="swarmOperations">The <see cref="ISwarmOperations"/> to use to validate the registration.</param>
		public static void Validate(this SwarmRegistration registration, ISwarmOperations swarmOperations)
		{
			if (registration == null)
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, $"Registration was null!"));

			ArgumentNullException.ThrowIfNull(swarmOperations);

			if (!swarmOperations.ValidateRegistration(registration))
				throw new RpcException(
					new Status(StatusCode.PermissionDenied, $"Registration was invalid!"));
		}
	}
}
