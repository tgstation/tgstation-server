using Tgstation.Server.Api;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Constants used by the swarm system.
	/// </summary>
	static class SwarmConstants
	{
		/// <summary>
		/// The base route for <see cref="Controllers.SwarmController"/>.
		/// </summary>
		public const string ControllerRoute = Routes.Root + "Swarm";

		/// <summary>
		/// The header used to pass in the <see cref="Configuration.SwarmConfiguration.PrivateKey"/>.
		/// </summary>
		public const string ApiKeyHeader = "X-API-KEY";

		/// <summary>
		/// The header used to pass in swarm registration IDs.
		/// </summary>
		public const string RegistrationIdHeader = "SwarmRegistration";

		/// <summary>
		/// The route used for swarm registration.
		/// </summary>
		public const string RegisterRoute = "Register";

		/// <summary>
		/// The route used for swarm updates.
		/// </summary>
		public const string UpdateRoute = "Update";
	}
}
