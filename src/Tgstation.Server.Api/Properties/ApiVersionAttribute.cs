using System;
using System.Reflection;

namespace Tgstation.Server.Api.Properties
{
	/// <summary>
	/// Attribute for bringing in the HTTP API version from MSBuild.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	sealed class ApiVersionAttribute : Attribute
	{
		/// <summary>
		/// Return the <see cref="Assembly"/>'s instance of the <see cref="ApiVersionAttribute"/>.
		/// </summary>
		public static ApiVersionAttribute Instance => Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<ApiVersionAttribute>();

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the TGS API definition.
		/// </summary>
		public string RawApiVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiVersionAttribute"/> class.
		/// </summary>
		/// <param name="rawApiVersion">The value of <see cref="RawApiVersion"/>.</param>
		public ApiVersionAttribute(
			string rawApiVersion)
		{
			RawApiVersion = rawApiVersion ?? throw new ArgumentNullException(nameof(rawApiVersion));
		}
	}
}
