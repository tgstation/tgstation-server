using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.Properties;

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
		/// Identifies the <see cref="Core.IServerAddressProvider.AddressEndPoint"/> of the server.
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
		/// The DMAPI <see cref="InteropVersion"/> being used.
		/// </summary>
		public static readonly Version InteropVersion = Version.Parse(MasterVersionsAttribute.Instance.RawInteropVersion);

		/// <summary>
		/// <see cref="JsonSerializerSettings"/> for use when communicating with the DMAPI.
		/// </summary>
		public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
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
