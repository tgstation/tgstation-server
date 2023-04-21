using System;
using System.Linq;

using Microsoft.AspNetCore.Http;

using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Controllers
{
	/// <inheritdoc />
	sealed class RequestSwarmRegistrationParser : IRequestSwarmRegistrationParser
	{
		/// <inheritdoc />
		public Guid GetRequestRegistrationId(HttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return Guid.Parse(request.Headers[SwarmConstants.RegistrationIdHeader].First());
		}
	}
}
