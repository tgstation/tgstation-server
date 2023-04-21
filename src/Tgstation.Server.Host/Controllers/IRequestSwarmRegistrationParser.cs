using System;

using Microsoft.AspNetCore.Http;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Parses the swarm registration header from a <see cref="HttpRequest"/>.
	/// </summary>
	public interface IRequestSwarmRegistrationParser
	{
		/// <summary>
		/// Gets the swarm registration <see cref="Guid"/> from the headers of a given <paramref name="request"/>.
		/// </summary>
		/// <param name="request">The <see cref="HttpRequest"/>, must contain a valid <see cref="Swarm.SwarmConstants.RegistrationIdHeader"/>.</param>
		/// <returns>The parsed registration ID.</returns>
		Guid GetRequestRegistrationId(HttpRequest request);
	}
}
