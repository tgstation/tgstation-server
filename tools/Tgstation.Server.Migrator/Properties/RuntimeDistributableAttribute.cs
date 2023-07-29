using System;
using System.Reflection;

namespace Tgstation.Server.Migrator.Properties
{
	/// <summary>
	/// Attribute for bringing in the runtime redistributable download link
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	sealed class RuntimeDistributableAttribute : Attribute
	{
		/// <summary>
		/// Return the <see cref="Assembly"/>'s instance of the <see cref="MasterVersionsAttribute"/>.
		/// </summary>
		public static RuntimeDistributableAttribute Instance => Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<RuntimeDistributableAttribute>()!;

		/// <summary>
		/// The <see cref="Uri"/> of the current runtime distributable.
		/// </summary>
		public Uri RuntimeDistributableUrl { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterVersionsAttribute"/> class.
		/// </summary>
		/// <param name="runtimeDistributableUrl">The <see cref="string"/> value of <see cref="RuntimeDistributableUrl"/>.</param>
		public RuntimeDistributableAttribute(
			string runtimeDistributableUrl)
		{
			RuntimeDistributableUrl = new Uri(runtimeDistributableUrl ?? throw new ArgumentNullException(nameof(runtimeDistributableUrl)));
		}
	}
}
