using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Shared;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Constants used for communication with the DMAPI.
	/// </summary>
	static class DMApiConstants
	{
		/// <summary>
		/// Identifies a DMAPI execution with the version as the value.
		/// </summary>
		public const string ParamApiVersion = "server_service_version";

		/// <summary>
		/// Identifies the <see cref="Core.IServerPortProvider.HttpApiPort"/> of the server.
		/// </summary>
		public const string ParamServerPort = "tgs_port";

		/// <summary>
		/// Identifies the <see cref="DMApiParameters.AccessIdentifier"/> for the session.
		/// </summary>
		public const string ParamAccessIdentifier = "tgs_key";

		/// <summary>
		/// Parameter json is encoded in for topic requests.
		/// </summary>
		public const string TopicData = "tgs_data";

		/// <summary>
		/// The maximum length of a BYOND side bridge request URL.
		/// </summary>
		/// <remarks>Testing has revealed that response size is effectively limited only by other factors like RAM.</remarks>
		public const uint MaximumBridgeRequestLength = 8198;

		/// <summary>
		/// The maximum length in bytes of a <see cref="Byond.TopicSender.ITopicClient"/> payload.
		/// </summary>
		public const uint MaximumTopicRequestLength = 65528;

		/// <summary>
		/// The maximum length in bytes of a <see cref="Byond.TopicSender.ITopicClient"/> response.
		/// </summary>
		public const uint MaximumTopicResponseLength = 65529;

		/// <summary>
		/// The DMAPI <see cref="InteropVersion"/> being used.
		/// </summary>
		public static readonly Version InteropVersion = Version.Parse(MasterVersionsAttribute.Instance.RawInteropVersion);

		/// <summary>
		/// <see cref="JsonSerializerSettings"/> for use when communicating with the DMAPI.
		/// </summary>
		public static readonly JsonSerializerSettings SerializerSettings = new()
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
