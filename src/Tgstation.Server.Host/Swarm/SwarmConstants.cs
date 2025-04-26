using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
		/// The authentication scheme and policy name used to validate swarm requests.
		/// </summary>
		public const string AuthenticationSchemeAndPolicy = "TGS_Swarm";

		/// <summary>
		/// Name of the <see cref="Controllers.SwarmTransferController"/>.
		/// </summary>
		public const string TransferControllerName = "SwarmTransfer";

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
		/// Number of seconds between a health check task triggering and a health check being performed.
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
