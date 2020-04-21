using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Constants used for communication with the DMAPI
	/// </summary>
	static class DMApiConstants
	{
		/// <summary>
		/// Identifies a DMAPI execution with the version as the value.
		/// </summary>
		public const string ParamApiVersion = "server_service_version";

		/// <summary>
		/// Identifies the path to the <see cref="Runtime.RuntimeInformation"/> file.
		/// </summary>
		public const string ParamDeploymentInformationFile = "tgs_json";

		/// <summary>
		/// Parameter json is encoded in for topic requests.
		/// </summary>
		public const string TopicData = "data";

		/// <summary>
		/// The DMAPI <see cref="Version"/> being used.
		/// </summary>
		public static readonly Version Version = new Version(5, 0, 0);

		/// <summary>
		/// <see cref="JsonSerializerSettings"/> for use when communicating with the DMAPI.
		/// </summary>
		public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Converters = new[]
			{
				new VersionConverter()
			},
			DefaultValueHandling = DefaultValueHandling.Ignore,
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		};
	}
}
