using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ComponentInterfacingController"/> for operations that require an instance.
	/// </summary>
	public abstract class InstanceRequiredController : ComponentInterfacingController
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceRequiredController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="ComponentInterfacingController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ComponentInterfacingController"/>.</param>
		protected InstanceRequiredController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<InstanceRequiredController> logger,
			IInstanceManager instanceManager,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders,
				  true)
		{
		}
	}
}
