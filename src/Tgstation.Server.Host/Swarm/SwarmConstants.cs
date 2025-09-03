using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Shared;

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
		public const string ControllerRoute = Routes.ApiRoot + "Swarm";

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
		/// The route used for swarm unregistration.
		/// </summary>
		public const string UnregisterRoute = "Unregister";

		/// <summary>
		/// The route used for swarm updates.
		/// </summary>
		public const string UpdateRoute = "Update";

		/// <summary>
		/// The route used for swarm updates to the initiation endpoint.
		/// </summary>
		public const string UpdateInitiationRoute = "UpdateInitiation";

		/// <summary>
		/// The route used for swarm updates to the abort endpoint.
		/// </summary>
		public const string UpdateAbortRoute = "UpdateAbort";

		/// <summary>
		/// Interval at which the swarm controller makes health checks on nodes.
		/// </summary>
		public const int ControllerHealthCheckIntervalMinutes = 3;

		/// <summary>
		/// Interval at which the node makes health checks on the controller if it has not received one.
		/// </summary>
		public const int NodeHealthCheckIntervalMinutes = 5;

		/// <summary>
		/// Number of minutes the controller waits to receive a ready-commit from all nodes before aborting an update.
		/// </summary>
		public const int UpdateCommitTimeoutMinutes = 10;

		/// <summary>
		/// Number of seconds between a health check <see cref="global::System.Threading.Tasks.TaskCompletionSource"/> triggering and a health check being performed.
		/// </summary>
		public const int SecondsToDelayForcedHealthChecks = 15;

		/// <summary>
		/// See <see cref="JsonSerializerSettings"/> for the swarm system.
		/// </summary>
		public static JsonSerializerSettings SerializerSettings { get; }

		/// <summary>
		/// Initializes static members of the <see cref="SwarmConstants"/> class.
		/// </summary>
		static SwarmConstants()
		{
			SerializerSettings = new()
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new CamelCaseNamingStrategy(),
				},
				Converters = new JsonConverter[]
				{
					new VersionConverter(),
					new BoolConverter(),
				},
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			};
		}
	}
}
