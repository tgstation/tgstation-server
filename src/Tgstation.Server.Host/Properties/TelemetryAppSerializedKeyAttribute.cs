using System;
using System.Reflection;

namespace Tgstation.Server.Host.Properties
{
	/// <summary>
	/// Attribute for bundling the GitHub App serialized private key used for version telemetry.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	sealed class TelemetryAppSerializedKeyAttribute : Attribute
	{
		/// <summary>
		/// Return the <see cref="Assembly"/>'s instance of the <see cref="TelemetryAppSerializedKeyAttribute"/>.
		/// </summary>
		public static TelemetryAppSerializedKeyAttribute? Instance => Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<TelemetryAppSerializedKeyAttribute>();

		/// <summary>
		/// The serialized GitHub App Client ID and private key.
		/// </summary>
		public string SerializedKey { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TelemetryAppSerializedKeyAttribute"/> class.
		/// </summary>
		/// <param name="serializedKey">The value of <see cref="SerializedKey"/>.</param>
		public TelemetryAppSerializedKeyAttribute(string serializedKey)
		{
			SerializedKey = serializedKey ?? throw new ArgumentNullException(nameof(serializedKey));
		}
	}
}
